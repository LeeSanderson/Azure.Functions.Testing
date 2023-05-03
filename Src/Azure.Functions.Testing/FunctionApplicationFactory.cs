using System.Net.Sockets;

namespace Azure.Functions.Testing;

public sealed class FunctionApplicationFactory : IDisposable
{
    private const int DefaultPort = 7071;

    private readonly bool _useHttps;
    private readonly int _port;
    private readonly List<Option> _options;
    private readonly bool _enableVerboseLogging;
    private readonly string _startupDirectory;
    private Executable? _funcExe;

    /// <summary>
    /// Create a factory for Azure Function testing
    /// </summary>
    /// <param name="functionLocator">Helper to locate the path of the function to execute</param>
    /// <param name="args">
    ///     Parameters to be passed to "func" to start the Function running locally.
    ///     <see href="https://learn.microsoft.com/en-us/azure/azure-functions/functions-core-tools-reference?tabs=v2#func-start"/>
    /// </param>
    public FunctionApplicationFactory(IFunctionLocator functionLocator, params string[] args)
    {
        _options = CommandLine.Parse(args).ToList();
        _useHttps = _options.Any(o => o.Key == "useHttps");
        _enableVerboseLogging = _options.Any(o => o.Key == "verbose");
        _port = EnsurePortAvailable();
        _startupDirectory = functionLocator.StartupDirectory;
        _options.Insert(0, new Option("start", "start"));
    }
    public void Dispose()
    {
        _funcExe?.Dispose();
        _funcExe = null;
    }

    /// <summary>
    /// Gets or sets the start up delay. If the Function does not start before this delay an exception will be thrown.
    /// </summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the health-check endpoint.
    /// If not null then the endpoint will be periodically polled on start up to make sure the Function is running.
    /// </summary>
    public string? HealthCheckEndpoint { get; set; }

    public TimeSpan ShutdownDelay { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan DefaultClientTimeout { get; set; } = TimeSpan.FromSeconds(100);

    public string? FuncExecutablePath { get; set; }

    public async Task<HttpClient> CreateClient()
    {
        await Start();
        return InternalCreateClient();
    }

    private HttpClient InternalCreateClient()
    {
        var protocol = _useHttps ? "https" : "http";
        var client = new HttpClient
        {
            BaseAddress = new Uri($"{protocol}://localhost:{_port}"),
            Timeout = DefaultClientTimeout
        };

        return client;
    }

    public async Task Start()
    {
        if (_funcExe != null)
        {
            return;
        }

        var funcExecutableName = GetFuncExecutableName();

        var args = ArgsToArgString(CommandLine.ReverseParse(_options.ToArray()));
        Log($"Executing '{funcExecutableName}' with arguments: {args}, in working directory: {_startupDirectory}");
        _funcExe = new Executable(funcExecutableName, args, workingDirectory: _startupDirectory);
        _funcExe.Start(
            o => Console.Out.WriteLine(o), 
            e => Console.Error.WriteLine(e));

        await TryStartFunction();
    }

    private async Task TryStartFunction()
    {
        var taskList = new Task[string.IsNullOrEmpty(HealthCheckEndpoint) ? 2 : 3];
        var exitCodeTask = _funcExe!.Process!.CreateWaitForExitTask();
        var cancellationTokenSource = new CancellationTokenSource();
        taskList[0] = exitCodeTask;
        var startupDelayTask = Task.Delay(StartupDelay, cancellationTokenSource.Token);
        taskList[1] = startupDelayTask;
        Task? healthCheckStartupTask = null;
        if (!string.IsNullOrEmpty(HealthCheckEndpoint))
        {
            healthCheckStartupTask = WaitForHealthCheck(cancellationTokenSource.Token);
            taskList[2] = healthCheckStartupTask;
        }

        await Task.WhenAny(taskList);
        var processCompleted = exitCodeTask.IsCompleted;
        if (processCompleted)
        {
            var exitCode = exitCodeTask.Result;
            throw new FunctionApplicationFactoryException(
                $"'func' failed to start (Exited prematurely with exit code {exitCode}). Check log for more details");
        }

        if (healthCheckStartupTask is {IsCompleted: true, IsFaulted: false})
        {
            Log($"Health check endpoint '{HealthCheckEndpoint}' indicated function has started");
        }

        cancellationTokenSource.Cancel();
    }

    private async Task WaitForHealthCheck(CancellationToken cancellationToken)
    {
        Log("Waiting for health endpoint to indicate function has started...");
        var healthCheckClient = InternalCreateClient();
        healthCheckClient.Timeout = TimeSpan.FromSeconds(5);

        var retryCount = 0;
        while (true)
        {
            try
            {
                var response = await healthCheckClient.GetAsync(HealthCheckEndpoint, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    Log($"Health endpoint return {response.StatusCode}. Retrying...");
                    await Task.Delay(1000, cancellationToken);
                    retryCount++;
                }
                else
                {
                    break;
                }
            }
            catch (TaskCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // Externally requested cancellation.
                    break;
                }

                // Probably healthCheckClient.Timeout cancellation.
                Log("Health endpoint timeout. Retrying...");
                retryCount++;
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException {SocketErrorCode: SocketError.ConnectionRefused})
                {
                    Log("Health endpoint return ConnectionRefused. Retrying...");
                    retryCount++;
                }
                else
                {
                    throw;
                }
            }

            if (retryCount > 10)
            {
                throw new FunctionApplicationFactoryException(
                    $"'func' health endpoint failed to return a success code 10 times. Check log for more details");
            }
        }
    }

    private string GetFuncExecutableName()
    {
        if (!string.IsNullOrEmpty(FuncExecutablePath))
        {
            if (!File.Exists(FuncExecutablePath))
            {
                throw new FunctionApplicationFactoryException(
                    $"Unable to find '{FuncExecutablePath}'. Make sure Azure Function core tools are installed. " +
                    "See: https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local");

            }

            return FuncExecutablePath!;
        }

        if (!CommandChecker.CommandExists("func"))
        {
            throw new FunctionApplicationFactoryException(
                "Unable to find 'func'. Make sure Azure Function core tools are installed. " +
                "See: https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local");
        }

        return "func";
    }

    public async Task Stop()
    {
        if (_funcExe == null)
        {
            return;
        }

        // Check the function is still running and hasn't stopped prematurely
        var (_, exitCode) = await _funcExe.TryGetExitCode(ShutdownDelay);
        if (exitCode > 0)
        {
            throw new FunctionApplicationFactoryException(
                $"'func' failed to stopped prematurely with exit code {exitCode}. Check log for more details");
        }

        _funcExe.Dispose();
        _funcExe = null;
    }

    private int EnsurePortAvailable()
    {
        var portIndex = _options.FindIndex(option => option.Key is "p" or "port");
        var port = DefaultPort;
        if (portIndex >= 0)
        {
            var portOption = _options[portIndex];
            if (!int.TryParse(portOption.Value, out port))
            {
                throw new FunctionApplicationFactoryException($"Invalid port parameter {portOption.Value}");
            }
        }

        if (!NetworkHelpers.IsPortAvailable(port))
        {
            var newPort = NetworkHelpers.GetAvailablePort();
            Log($"Switching from port {port} to {newPort} as port {port} is unavailable");
            port = newPort;
            var newPortOption = new Option("port", "--port", port.ToString());
            if (portIndex >= 0)
            {
                _options.RemoveAt(portIndex);
            }

            _options.Add(newPortOption);
        }

        return port;
    }

    private void Log(string message)
    {
        if (_enableVerboseLogging)
        {
            Console.WriteLine(message);
        }
    }

    private string? ArgsToArgString(string[] args)
    {
        if (args.Length == 0) return null;

        return string.Join(" ", args.Select(arg => arg.Any(char.IsWhiteSpace) ? "\"" + arg + "\"" : arg ));
    }
}