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
            using var factory = CreateFunctionApplicationFactory();

            // Delay to wait before function is considered up and running. 
            // Can be adjusted depending on build time of Function project
            // Can be augmented by HealthCheckEndpoint if one is available
            factory.StartupDelay = TimeSpan.FromSeconds(20);

            // Default timeout can be adjusted - defaults to 100 seconds
            factory.DefaultClientTimeout = TimeSpan.FromSeconds(20);

            // Shutdown delay allows time for func process to stop - defaults to 1 second
            // but can be increased if required
            factory.ShutdownDelay = TimeSpan.FromSeconds(2);

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

        [Fact]
        public async Task HelloIsOkayWhenWaitingForHealthCheckToStart()
        {
            using var factory = CreateFunctionApplicationFactory();

            // The combination of StartupDelay and HealthCheckEndpoint work as follows during startup:
            // 1. HealthCheckEndpoint is periodically polled, and function is considered started if endpoint returns a success response.
            // 2. If the StartupDelay is exceeded the function is assumed to have started (even if the health check has not succeeded)
            // Adjust depending on build time of Function project
            factory.StartupDelay = TimeSpan.FromSeconds(20); 
            factory.HealthCheckEndpoint = GetHelloUri;

            using var client = await factory.CreateClient();

            var response = await client.GetAsync(GetHelloUri);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private static FunctionApplicationFactory CreateFunctionApplicationFactory() =>
            new(FunctionLocator.FromProject("Dotnet.Function.Demo"), "--verbose", "--debug", "--csharp");
    }
}