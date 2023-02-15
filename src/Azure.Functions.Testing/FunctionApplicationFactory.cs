namespace Azure.Functions.Testing;

public class FunctionApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    private const int DefaultPort = 7071;

    private readonly bool _useHttps;
    private readonly int _port;

    public FunctionApplicationFactory(params string[] commandLineArgs)
    {
        _useHttps = commandLineArgs.Contains("--useHttps");
        _port = ProcessPort(commandLineArgs);
    }

    public HttpClient CreateClient()
    {
        var protocol = _useHttps ? "https" : "http";
        var client = new HttpClient
        {
            BaseAddress = new Uri($"{protocol}://localhost:{_port}"),
            Timeout = default
        };

        return client;
    }

    private int ProcessPort(string[] commandLineArgs)
    {
        throw new NotImplementedException();
        /*
        var portIndex = Array.FindIndex(commandLineArgs, arg => arg is "-p" or "--p" or "-port" or "--port");
        var port = DefaultPort;
        if (portIndex < 0)
        {
            var portValueIndex = portIndex + 1;
            if (portValueIndex > ) { }
        }*/
    }
 }