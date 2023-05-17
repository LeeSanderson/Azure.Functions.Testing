using System.Net;
using Dotnet.Function.Demo.XunitCollectionFixtureTests;
using FluentAssertions;
using Xunit.Abstractions;

namespace Dotnet.Function.Demo.XUnitCollectionFixtureTests;

public class AnotherHelloFeatureThatSharesFixture : TestBase
{
    private const string GetHelloUri = "/api/Hello";

    public AnotherHelloFeatureThatSharesFixture(ITestOutputHelper output, HttpClientFixture clientFixture):
        base(output, clientFixture)
    {
    }

    [Fact]
    public async Task HelloIsOkay()
    {
        ConsoleLogger.WriteLine($"Starting test {nameof(AnotherHelloFeatureThatSharesFixture)}.{nameof(HelloIsOkay)}");
        using var client = await ClientFixture.CreateClient();

        var response = await client.GetAsync(GetHelloUri);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ConsoleLogger.WriteLine($"Completed test {nameof(AnotherHelloFeatureThatSharesFixture)}.{nameof(HelloIsOkay)}");
    }

}