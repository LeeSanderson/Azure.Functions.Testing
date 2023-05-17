namespace Azure.Functions.Testing;

public static class OsDetector
{
    public static bool IsOnWindows()
    {
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.WinCE:
            case PlatformID.Xbox:
                return true;
            default:
                return false;
        }
    }
}