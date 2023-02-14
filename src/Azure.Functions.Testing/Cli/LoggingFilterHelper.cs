using Azure.Functions.Testing.Cli.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure.Functions.Testing.Cli;

public class LoggingFilterHelper
{
    // CI EnvironmentSettings
    // https://github.com/watson/ci-info/blob/master/index.js#L52-L59
    public const string Ci = "CI"; // Travis CI, CircleCI, Cirrus CI, Gitlab CI, Appveyor, CodeShip, dsari
    public const string CiContinuousIntegration = "CONTINUOUS_INTEGRATION";  // Travis CI, Cirrus CI
    public const string CiBuildNumber = "BUILD_NUMBER";  // Travis CI, Cirrus CI
    public const string CiRunId = "RUN_ID"; // TaskCluster, dsari

    public LoggingFilterHelper(IConfigurationRoot hostJsonConfig, bool? verboseLogging)
    {
        VerboseLogging = verboseLogging.HasValue && verboseLogging.Value;

        if (IsCiEnvironment(verboseLogging.HasValue))
        {
            VerboseLogging = true;
        }
        if (VerboseLogging)
        {
            SystemLogDefaultLogLevel = LogLevel.Information;
        }
        if (Utilities.LogLevelExists(hostJsonConfig, Utilities.LogLevelDefaultSection, out LogLevel logLevel))
        {
            SystemLogDefaultLogLevel = logLevel;
            UserLogDefaultLogLevel = logLevel;
        }
    }

    /// <summary>
    /// Default level for system logs
    /// </summary>
    public LogLevel SystemLogDefaultLogLevel { get; } = LogLevel.Warning;

    /// <summary>
    /// Default level for user logs
    /// </summary>
    public LogLevel UserLogDefaultLogLevel { get; } = LogLevel.Information;

    /// <summary>
    /// Is set to true if `func start` is started with `--verbose` flag. If set, SystemLogDefaultLogLevel is set to Information
    /// </summary>
    public bool VerboseLogging { get; private set; }

    internal bool IsCiEnvironment(bool verboseLoggingArgExists)
    {
        if (verboseLoggingArgExists)
        {
            return VerboseLogging;
        }
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Ci)) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(CiContinuousIntegration)) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(CiBuildNumber)) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(CiRunId)))
        {
            return true;
        }
        return false;
    }
}
