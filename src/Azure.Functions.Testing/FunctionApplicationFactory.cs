using Azure.Functions.Testing.Cli;
using Azure.Functions.Testing.Cli.Actions.HostActions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Azure.Functions.Testing
{
    public class FunctionApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
        where TEntryPoint : class
    {
        private readonly StartHostAction _startHostAction;

        public FunctionApplicationFactory(params string[] commandLineArgs)
        {
            var initialization = new CliInitialization();
            initialization.Init(commandLineArgs);
            _startHostAction = new StartHostAction(initialization.SecretManager);
            _startHostAction.ParseArgs(commandLineArgs);
        }

        protected override IHostBuilder? CreateHostBuilder() => null;

        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            var builder = Task.Run(() => _startHostAction.BuildWebHostBuilder()).Result;
            builder.UseEnvironment(Environments.Development);
            return builder;
        }
    }
}
