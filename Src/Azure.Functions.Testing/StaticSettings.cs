namespace Azure.Functions.Testing;

public static class StaticSettings
{
    public static bool IsDebug
    {
        get => Environment.GetEnvironmentVariable(Constants.CliDebug) == "1";
        set => Environment.SetEnvironmentVariable(Constants.CliDebug, value ? "1" : "0");
    }
}