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

    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan ShutdownDelay { get; set; } = TimeSpan.FromSeconds(1);

    private TimeSpan DefaultClientTimeout { get; set; } = TimeSpan.FromSeconds(100);

    public async Task<HttpClient> CreateClient()
    {
        await Start();
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

        if (!CommandChecker.CommandExists("func"))
        {
            throw new FunctionApplicationFactoryException(
                "Unable to find 'func'. Make sure Azure Function core tools are installed. " + 
                "See: https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local");
        }

        var args = ArgsToArgString(CommandLine.ReverseParse(_options.ToArray()));
        Log($"Executing 'func' with arguments: {args}, in working directory: {_startupDirectory}");
        _funcExe = new Executable("func", args, workingDirectory: _startupDirectory);
        _funcExe.Start(
            o => Console.Out.WriteLine(o), 
            e => Console.Error.WriteLine(e));

        var (exited, exitCode) = await _funcExe.TryGetExitCode(StartupDelay);
        if (exited)
        {
            throw new FunctionApplicationFactoryException(
                $"'func' failed to start (Exited prematurely with exit code {exitCode}). Check log for more details");
        }
    }

    public async Task Stop()
    {
        if (_funcExe == null)
        {
            return;
        }

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

        return string.Join(" ", args.Select(arg => arg.Any(Char.IsWhiteSpace) ? "\"" + arg + "\"" : arg ));
    }
}