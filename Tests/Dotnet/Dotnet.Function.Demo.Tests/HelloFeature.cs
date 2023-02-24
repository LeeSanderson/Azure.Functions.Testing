using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using Xunit.Abstractions;
using Xunit.Shared;
using Azure.Functions.Testing;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dotnet.Function.Demo.Tests
{
    public class HelloFeature
    {
        private const string GetHelloUri = "/api/Hello";

        public HelloFeature(ITestOutputHelper output)
        {
            Console.SetOut(new TestOutputConsoleAdapter(output));
        }

        [Fact]
        public async Task HelloIsOkay()
        {
            // using var factory = new FunctionApplicationFactory(
            //     FunctionLocator.FromProject("Dotnet.Function.Demo"), "--verbose", "--debug", "--no-build", "--prefix", "bin/Debug/net7.0");
            using var factory = new FunctionApplicationFactory(
                FunctionLocator.FromProject("Dotnet.Function.Demo"), "--verbose", "--debug", "--csharp");
            factory.StartupDelay = TimeSpan.FromSeconds(20); // Adjust depending on build time of Function project
            using var client = await factory.CreateClient();

            var response = await client.GetAsync(GetHelloUri);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CannotTestFunctionUsingWebApplicationFactory()
        {
            await using var factory = new WebApplicationFactory<Program>();

            factory.Invoking(f => f.CreateClient()).Should().Throw<InvalidOperationException>();
        }
    }
}