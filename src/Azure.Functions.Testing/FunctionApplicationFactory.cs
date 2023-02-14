namespace Azure.Functions.Testing;

public class FunctionApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    public FunctionApplicationFactory(params string[] commandLineArgs)
    {
    }

    public HttpClient CreateClient()
    {
        throw new NotImplementedException();
    }
}