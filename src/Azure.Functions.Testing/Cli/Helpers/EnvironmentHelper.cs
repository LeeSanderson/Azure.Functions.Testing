namespace Azure.Functions.Testing.Cli.Helpers
{
    static class EnvironmentHelper
    {
        public static bool GetEnvironmentVariableAsBool(string keyName)
        {
            var val = Environment.GetEnvironmentVariable(keyName);
            if (string.IsNullOrEmpty(val))
            {
                return false;
            }

            return val.Equals("1") || val.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public static void SetEnvironmentVariableAsBoolIfNotExists(string keyName)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(keyName)))
            {
                Environment.SetEnvironmentVariable(keyName, "true");
            }
        }
    }
}
