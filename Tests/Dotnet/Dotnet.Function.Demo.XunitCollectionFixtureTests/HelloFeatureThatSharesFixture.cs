using Dotnet.Function.Demo.XunitCollectionFixtureTests;
using System.Net;
using Xunit.Abstractions;
using Xunit.Shared;
using FluentAssertions;

namespace Dotnet.Function.Demo.XUnitCollectionFixtureTests;


[Collection(nameof(HttpClientFixture))]
public class HelloFeatureThatSharesFixture
{
    private const string GetHelloUri = "/api/Hello";

    private readonly HttpClientFixture _clientFixture;

    public HelloFeatureThatSharesFixture(ITestOutputHelper output, HttpClientFixture clientFixture)
    {
        _clientFixture = clientFixture;
        Console.SetOut(new TestOutputConsoleAdapter(output));
    }

    [Fact]
    public async Task HelloIsOkay()
    {
        using var client = await _clientFixture.CreateClient();

        var response = await client.GetAsync(GetHelloUri);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

}