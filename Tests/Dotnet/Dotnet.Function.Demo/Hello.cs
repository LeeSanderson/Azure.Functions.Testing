using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace Dotnet.Function.Demo
{
    public static class Hello
    {
        [FunctionName("Hello")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            return new OkObjectResult("Hello");
        }

        [FunctionName("SlowHello")]
        public static async Task<IActionResult> SlowHelloRun(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            return new OkObjectResult("Hello");
        }

    }
}
