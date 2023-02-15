using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using Xunit.Abstractions;
using Azure.Functions.Testing;
using FluentAssertions;

namespace Dotnet.Function.Demo.Tests
{
    public class HelloFeature
    {
        private readonly HttpClient _client;

        public HelloFeature(ITestOutputHelper output)
        {
            Console.SetOut(new TestOutputConsoleAdapter(output));

            FunctionApplicationFactory<Program> factory = new("--verbose", "--debug", "no-build");
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task HelloIsOkay()
        {
            const string uri = "/api/Hello";
            
            var response = await _client.GetAsync(uri);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}