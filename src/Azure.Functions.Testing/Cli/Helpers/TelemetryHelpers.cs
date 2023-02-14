using System.Reflection;
using Azure.Functions.Testing.Cli.Common;
using Azure.Functions.Testing.Cli.Telemetry;
using Colors.Net;
using Fclp.Internals;

namespace Azure.Functions.Testing.Cli.Helpers
{
    internal static class TelemetryHelpers
    {
        public static IEnumerable<string> GetCommandsFromCommandLineOptions(IEnumerable<ICommandLineOption> options)
        {
            return options.Select(option => option.HasLongName ? option.LongName : option.ShortName);
        }

        public static void AddCommandEventToDictionary(IDictionary<string, string> events, string eventName, string eventVal)
        {
            if (StaticSettings.IsTelemetryEnabled)
            {
                try
                {
                    events[eventName] = eventVal;
                }
                catch (Exception ex)
                {
                    if (StaticSettings.IsDebug)
                    {
                        ColoredConsole.Error.WriteLine(ex.ToString());
                    }
                }
            }
        }

        public static void UpdateTelemetryEvent(TelemetryEvent telemetryEvent, IDictionary<string, string> commandEvents)
        {
            try
            {
                var languageContext = GlobalCoreToolsSettings.CurrentLanguageOrNull ?? "N/A";
                telemetryEvent.GlobalSettings["language"] = languageContext;
                telemetryEvent.CommandEvents = commandEvents;
            }
            catch (Exception ex)
            {
                if (StaticSettings.IsDebug)
                {
                    ColoredConsole.Error.WriteLine(ex.ToString());
                }
            }
        }

        public static bool CheckIfTelemetryEnabled()
        {
            // If key is not set, can't get anything
            // Note: Do not change this to use from the Constants file. As the key in Constants may change
            if (Constants.TelemetryInstrumentationKey == "00000000-0000-0000-0000-000000000000")
            {
                return false;
            }

            #pragma warning disable 0162
            // If opt out is not set, we check if the default sentinel is present
            // ReSharper disable HeuristicUnreachableCode
            var optOutVar = Environment.GetEnvironmentVariable(Constants.TelemetryOptOutVariable);
            if (string.IsNullOrEmpty(optOutVar))
            {
                var sentinelPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "telemetryDefaultOn.sentinel");
                return File.Exists(sentinelPath);
            }

            // If opt out is present and set to falsy, only then we enable telemetry
            return optOutVar == "0" || optOutVar.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        public static void LogEventIfAllowedSafe(ITelemetry telemetry, TelemetryEvent telemetryEvent)
        {
            try
            {
                LogEventIfAllowed(telemetry, telemetryEvent);
            }
            catch
            {
                // oh well!
            }
        }

        public static void LogEventIfAllowed(ITelemetry telemetry, TelemetryEvent telemetryEvent)
        {
            if (!telemetry.Enabled)
            {
                return;
            }

            var properties = new Dictionary<string, string>
            {
                { "commandName" , telemetryEvent.CommandName },
                { "iActionName" , telemetryEvent.IActionName },
                { "parameters" , string.Join(",", telemetryEvent.Parameters) },
                { "prefixOrScriptRoot" , telemetryEvent.PrefixOrScriptRoot.ToString() },
                { "parseError" , telemetryEvent.ParseError.ToString() },
                { "isSuccessful" , telemetryEvent.IsSuccessful.ToString() }
            };

            foreach (KeyValuePair<string, string> keyValue in telemetryEvent.CommandEvents)
            {
                properties[keyValue.Key] = keyValue.Value;
            }

            foreach (KeyValuePair<string, string> keyValue in telemetryEvent.GlobalSettings)
            {
                properties[$"global_{keyValue.Key}"] = keyValue.Value;
            }

            var measurements = new Dictionary<string, double>
            {
                { "timeTaken" , telemetryEvent.TimeTaken }
            };

            telemetry.TrackEvent(telemetryEvent.CommandName, properties, measurements);
            telemetry.Flush();
        }
    }
}
