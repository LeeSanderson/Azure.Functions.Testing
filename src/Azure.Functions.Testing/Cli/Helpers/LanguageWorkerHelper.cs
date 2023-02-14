using System.Runtime.InteropServices;

namespace Azure.Functions.Testing.Cli.Helpers;

public static class LanguageWorkerHelper
{
    private static readonly Dictionary<WorkerRuntime, string> Map = new Dictionary<WorkerRuntime, string>
        {
            { WorkerRuntime.node, "languageWorkers:node:arguments" },
            { WorkerRuntime.python, "languageWorkers:python:arguments" },
            { WorkerRuntime.java, "languageWorkers:java:arguments" },
            // ReSharper disable once StringLiteralTypo
            { WorkerRuntime.powershell, "languageWorkers:powershell:arguments" },
            { WorkerRuntime.dotnet, string.Empty },
            { WorkerRuntime.custom, string.Empty },
            { WorkerRuntime.None, string.Empty }
        }
        .Select(p => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? p
            : new KeyValuePair<WorkerRuntime, string>(p.Key, p.Value.Replace(":", "__")))
        .ToDictionary(k => k.Key, v => v.Value);

    public static IReadOnlyDictionary<string, string?> GetWorkerConfiguration(string value)
    {
        if (Map.ContainsKey(GlobalCoreToolsSettings.CurrentWorkerRuntime) && !string.IsNullOrWhiteSpace(value))
        {
            return new Dictionary<string, string?>
            {
                { Map[GlobalCoreToolsSettings.CurrentWorkerRuntime], value }
            };
        }

        return new Dictionary<string, string?>();
    }
}
