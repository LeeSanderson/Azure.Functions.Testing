using Azure.Functions.Testing.Cli.Helpers;

namespace Azure.Functions.Testing.Cli.Common
{
    public static class StaticSettings
    {
        private static readonly Lazy<bool> IsTelemetryEnabledCache = new(TelemetryHelpers.CheckIfTelemetryEnabled);

        public static bool IsDebug => Environment.GetEnvironmentVariable(Constants.CliDebug) == "1";

        public static bool IsTelemetryEnabled
        {
            get
            {
                return IsTelemetryEnabledCache.Value;
            }
        }
    }
}
