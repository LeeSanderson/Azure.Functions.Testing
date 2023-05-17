using Xunit.Abstractions;
using Xunit.Shared;

namespace Dotnet.Function.Demo.XunitCollectionFixtureTests;

[Collection(nameof(HttpClientFixture))]
public abstract class TestBase : IDisposable
{
    protected readonly TestOutputConsoleAdapter ConsoleLogger;
    protected readonly HttpClientFixture ClientFixture;

    protected TestBase(ITestOutputHelper output, HttpClientFixture clientFixture)
    {
        ClientFixture = clientFixture;
        ConsoleLogger = new TestOutputConsoleAdapter(output);
    }

    public void Dispose()
    {
        ConsoleLogger.Dispose();
    }
}