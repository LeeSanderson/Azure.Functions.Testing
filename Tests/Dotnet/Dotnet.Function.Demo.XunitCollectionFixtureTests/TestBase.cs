using Xunit.Abstractions;
using Xunit.Shared;

namespace Dotnet.Function.Demo.XunitCollectionFixtureTests;

[Collection(nameof(HttpClientFixture))]
public abstract class TestBase : IDisposable
{
    private readonly TestOutputConsoleAdapter _outputConsoleAdapter;
    protected readonly HttpClientFixture ClientFixture;

    protected TestBase(ITestOutputHelper output, HttpClientFixture clientFixture)
    {
        ClientFixture = clientFixture;
        _outputConsoleAdapter = new TestOutputConsoleAdapter(output);
    }

    public void Dispose()
    {
        _outputConsoleAdapter.Dispose();
    }
}