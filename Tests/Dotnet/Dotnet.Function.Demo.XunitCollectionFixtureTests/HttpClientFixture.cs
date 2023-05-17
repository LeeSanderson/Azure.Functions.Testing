using Azure.Functions.Testing;
using Xunit.Abstractions;
using Xunit.Shared;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Dotnet.Function.Demo.XunitCollectionFixtureTests;

// ReSharper disable once ClassNeverInstantiated.Global
public class HttpClientFixture : IDisposable, IAsyncLifetime
{
    private readonly FunctionApplicationFactory _factory;
    private readonly MessageSinkConsoleAdapter _messageSinkConsoleAdapter;

    public HttpClientFixture(IMessageSink diagnosticMessageSink)
    {
        // Create factory for local testing. Could use environment variables to switch between local
        // testing and testing a deployed function (just need to create a HTTP client with a BaseAddress)
        _factory = new FunctionApplicationFactory(
            FunctionLocator.FromProject("Dotnet.Function.Demo"), "--verbose", "--debug", "--csharp");
        _messageSinkConsoleAdapter = new MessageSinkConsoleAdapter(diagnosticMessageSink);
    }

    public async Task<HttpClient> CreateClient() => await _factory.CreateClient().ConfigureAwait(false);

    public void Dispose()
    {
        _messageSinkConsoleAdapter.Dispose();
        _factory.Dispose();
    }

    public Task InitializeAsync()
    {
        // Set startup timeout. Adjust depending on build time of Function project;
        _factory.StartupDelay = TimeSpan.FromSeconds(20);
        _factory.KillAllFuncProcesses();
        return _factory.Start();
    }

    public Task DisposeAsync()
    {
        return _factory.Stop();
    }
}

[CollectionDefinition(nameof(HttpClientFixture))]
public class HttpClientCollection : ICollectionFixture<HttpClientFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}