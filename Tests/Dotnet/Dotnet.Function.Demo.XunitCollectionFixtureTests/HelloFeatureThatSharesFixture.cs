using Dotnet.Function.Demo.XunitCollectionFixtureTests;
using System.Net;
using Xunit.Abstractions;
using FluentAssertions;

namespace Dotnet.Function.Demo.XUnitCollectionFixtureTests;

public class HelloFeatureThatSharesFixture : TestBase
{
    private const string GetHelloUri = "/api/Hello";

    public HelloFeatureThatSharesFixture(ITestOutputHelper output, HttpClientFixture clientFixture):
        base(output, clientFixture)
    {
    }

    [Fact]
    public async Task HelloIsOkay()
    {
        ConsoleLogger.WriteLine($"Starting test {nameof(HelloFeatureThatSharesFixture)}.{nameof(HelloIsOkay)}");
        using var client = await ClientFixture.CreateClient();

        var response = await client.GetAsync(GetHelloUri);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ConsoleLogger.WriteLine($"Completed test {nameof(HelloFeatureThatSharesFixture)}.{nameof(HelloIsOkay)}");
    }
}