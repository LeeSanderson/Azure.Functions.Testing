using Azure.Functions.Testing;

namespace Dotnet.Function.Demo.XunitCollectionFixtureTests;

public class HttpClientFixture : IDisposable
{
    private readonly FunctionApplicationFactory _factory;

    public HttpClientFixture()
    {
        // Create factory for local testing. Could use environment variables to switch between local
        // testing and testing a deployed function (just need to create a HTTP client with a BaseAddress)
        _factory = new FunctionApplicationFactory(
            FunctionLocator.FromProject("Dotnet.Function.Demo"), "--verbose", "--debug");


        // Set startup timeout. Adjust depending on build time of Function project;
        _factory.StartupDelay = TimeSpan.FromSeconds(5); 
    }

    public async Task<HttpClient> CreateClient() => await _factory.CreateClient().ConfigureAwait(false);

    public void Dispose()
    {
        _factory.Dispose();
    }
}

[CollectionDefinition(nameof(HttpClientFixture))]
public class HttpClientCollection : ICollectionFixture<HttpClientFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}