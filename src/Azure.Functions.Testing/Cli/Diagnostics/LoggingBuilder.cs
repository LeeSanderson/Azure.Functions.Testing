using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Azure.WebJobs.Script;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Functions.Testing.Cli.Common;

namespace Azure.Functions.Testing.Cli.Diagnostics
{
    internal class LoggingBuilder : IConfigureBuilder<ILoggingBuilder>
    {
        private readonly LoggingFilterHelper _loggingFilterHelper;
        private readonly string? _jsonOutputFile;

        public LoggingBuilder(LoggingFilterHelper loggingFilterHelper, string? jsonOutputFile)
        {
            _loggingFilterHelper = loggingFilterHelper;
            _jsonOutputFile = jsonOutputFile;
        }

        public void Configure(ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider>(p =>
            {
                //Cache LoggerFilterOptions to be used by the logger to filter logs based on content
                var filterOptions = p.GetService<IOptions<LoggerFilterOptions>>();
                return new ColoredConsoleLoggerProvider(_loggingFilterHelper, filterOptions?.Value!, _jsonOutputFile);
            });

            builder.AddFilter<ColoredConsoleLoggerProvider>((_, _) => true);

            builder.Services.AddSingleton<TelemetryClient>(provider =>
            {
                var configuration = provider.GetService<TelemetryConfiguration>();
                TelemetryClient client = new TelemetryClient(configuration);

                // ReSharper disable once StringLiteralTypo
                client.Context.GetInternalContext().SdkVersion = $"azurefunctionscoretools: {Constants.CliVersion}";

                return client;
            });
        }
    }
}
