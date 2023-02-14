using Azure.Functions.Testing.Cli.Common;
using Azure.Functions.Testing.Cli.Interfaces;
using Colors.Net;
using static Azure.Functions.Testing.Cli.Common.OutputTheme;

namespace Azure.Functions.Testing.Cli.Helpers
{
    public static class WorkerRuntimeLanguageHelper
    {
        private static readonly IDictionary<WorkerRuntime, IEnumerable<string>> AvailableWorkersRuntime = new Dictionary<WorkerRuntime, IEnumerable<string>>
        {
            { WorkerRuntime.dotnet, new [] { "c#", "csharp", "f#", "fsharp" } },
            { WorkerRuntime.dotnetIsolated, new [] { "dotnet-isolated", "c#-isolated", "csharp-isolated", "f#-isolated", "fsharp-isolated" } },
            { WorkerRuntime.node, new [] { "js", "javascript", "typescript", "ts" } },
            { WorkerRuntime.python, new []  { "py" } },
            { WorkerRuntime.java, new string[] { } },
            // ReSharper disable once StringLiteralTypo
            { WorkerRuntime.powershell, new [] { "pwsh" } },
            { WorkerRuntime.custom, new string[] { } }
        };

        private static readonly IDictionary<string, WorkerRuntime> NormalizeMap = AvailableWorkersRuntime
            .Select(p => p.Value.Select(v => new { key = v, value = p.Key }).Append(new { key = p.Key.ToString(), value = p.Key }))
            .SelectMany(i => i)
            .ToDictionary(k => k.key, v => v.value, StringComparer.OrdinalIgnoreCase);

        private static readonly IDictionary<WorkerRuntime, string> WorkerToDefaultLanguageMap = new Dictionary<WorkerRuntime, string>
        {
            { WorkerRuntime.dotnet, Constants.Languages.CSharp },
            { WorkerRuntime.dotnetIsolated, Constants.Languages.CSharpIsolated },
            { WorkerRuntime.node, Constants.Languages.JavaScript },
            { WorkerRuntime.python, Constants.Languages.Python },
            { WorkerRuntime.powershell, Constants.Languages.Powershell },
            { WorkerRuntime.custom, Constants.Languages.Custom },
        };

        private static readonly IDictionary<string, IEnumerable<string>> LanguageToAlias = new Dictionary<string, IEnumerable<string>>
        {
            // By default node should map to javascript
            { Constants.Languages.JavaScript, new [] { "js", "node" } },
            { Constants.Languages.TypeScript, new [] { "ts" } },
            { Constants.Languages.Python, new [] { "py" } },
            // ReSharper disable once StringLiteralTypo
            { Constants.Languages.Powershell, new [] { "pwsh" } },
            { Constants.Languages.CSharp, new [] { "csharp", "dotnet" } },
            { Constants.Languages.CSharpIsolated, new [] { "dotnet-isolated", "dotnetIsolated" } },
            { Constants.Languages.Java, new string[] { } },
            { Constants.Languages.Custom, new string[] { } }
        };

        public static readonly IDictionary<string, string> WorkerRuntimeStringToLanguage = LanguageToAlias
            .Select(p => p.Value.Select(v => new { key = v, value = p.Key }).Append(new { key = p.Key.ToString(), value = p.Key }))
            .SelectMany(i => i)
            .ToDictionary(k => k.key, v => v.value, StringComparer.OrdinalIgnoreCase);

        public static readonly IDictionary<WorkerRuntime, IEnumerable<string>> WorkerToSupportedLanguages = new Dictionary<WorkerRuntime, IEnumerable<string>>
        {
            { WorkerRuntime.node, new [] { Constants.Languages.JavaScript, Constants.Languages.TypeScript } },
            { WorkerRuntime.dotnet, new [] { Constants.Languages.CSharp, Constants.Languages.FSharp } },
            { WorkerRuntime.dotnetIsolated, new [] { Constants.Languages.CSharpIsolated, Constants.Languages.FSharpIsolated } }
        };

        public static string AvailableWorkersRuntimeString =>
            string.Join(", ", AvailableWorkersRuntime.Keys
                .Where(k => (k != WorkerRuntime.java))
                .Select(s => s.ToString()));

        public static string GetRuntimeMoniker(WorkerRuntime workerRuntime)
        {
            switch (workerRuntime)
            {
                case WorkerRuntime.None:
                    return "None";
                case WorkerRuntime.dotnet:
                    return "dotnet";
                case WorkerRuntime.dotnetIsolated:
                    return "dotnet-isolated";
                case WorkerRuntime.node:
                    return "node";
                case WorkerRuntime.python:
                    return "python";
                case WorkerRuntime.java:
                    return "java";
                case WorkerRuntime.powershell:
                    // ReSharper disable once StringLiteralTypo
                    return "powershell";
                case WorkerRuntime.custom:
                    return "custom";
                default:
                    return "None";
            }
        }

        public static IDictionary<WorkerRuntime, string> GetWorkerToDisplayStrings()
        {
            IDictionary<WorkerRuntime, string> workerToDisplayStrings = new Dictionary<WorkerRuntime, string>();
            foreach (WorkerRuntime wr in AvailableWorkersList)
            {
                switch (wr)
                {
                    case WorkerRuntime.dotnetIsolated:
                        workerToDisplayStrings[wr] = "dotnet (isolated process)";
                        break;
                    default:
                        workerToDisplayStrings[wr] = wr.ToString();
                        break;
                }
            }
            return workerToDisplayStrings;
        }

        public static IEnumerable<WorkerRuntime> AvailableWorkersList => AvailableWorkersRuntime.Keys
            .Where(k => k != WorkerRuntime.java);

        public static WorkerRuntime NormalizeWorkerRuntime(string workerRuntime)
        {
            if (string.IsNullOrWhiteSpace(workerRuntime))
            {
                throw new ArgumentNullException(nameof(workerRuntime), "worker runtime can't be empty");
            }
            else if (NormalizeMap.ContainsKey(workerRuntime))
            {
                return NormalizeMap[workerRuntime];
            }
            else
            {
                throw new ArgumentException($"Worker runtime '{workerRuntime}' is not a valid option. Options are {AvailableWorkersRuntimeString}");
            }
        }

        public static string NormalizeLanguage(string languageString)
        {
            if (string.IsNullOrWhiteSpace(languageString))
            {
                throw new ArgumentNullException(nameof(languageString), "language can't be empty");
            }
            else if (NormalizeMap.ContainsKey(languageString))
            {
                return WorkerRuntimeStringToLanguage[languageString];
            }
            else
            {
                throw new ArgumentException($"Language '{languageString}' is not available. Available language strings are {WorkerRuntimeStringToLanguage.Keys}");
            }
        }

        public static IEnumerable<string> LanguagesForWorker(WorkerRuntime worker)
        {
            return NormalizeMap.Where(p => p.Value == worker).Select(p => p.Key);
        }

        public static WorkerRuntime GetCurrentWorkerRuntimeLanguage(ISecretsManager secretsManager)
        {
            var setting = secretsManager.GetSecrets().FirstOrDefault(s => s.Key.Equals(Constants.FunctionsWorkerRuntime, StringComparison.OrdinalIgnoreCase)).Value;
            try
            {
                return NormalizeWorkerRuntime(setting);
            }
            catch
            {
                return WorkerRuntime.None;
            }
        }

        internal static WorkerRuntime SetWorkerRuntime(ISecretsManager secretsManager, string language)
        {
            var worker = NormalizeWorkerRuntime(language);

            secretsManager.SetSecret(Constants.FunctionsWorkerRuntime, worker.ToString());
            ColoredConsole
                .WriteLine(WarningColor("Starting from 2.0.1-beta.26 it's required to set a language for your project in your settings"))
                .WriteLine(WarningColor($"'{worker}' has been set in your local.settings.json"));

            return worker;
        }

        public static string GetDefaultTemplateLanguageFromWorker(WorkerRuntime worker)
        {
            if (!WorkerToDefaultLanguageMap.ContainsKey(worker))
            {
                throw new ArgumentException($"Worker runtime '{worker}' is not a valid worker for a template.");
            }
            return WorkerToDefaultLanguageMap[worker];
        }

        public static bool IsDotnet(WorkerRuntime worker)
        {
            return worker == WorkerRuntime.dotnet || worker == WorkerRuntime.dotnetIsolated;
        }
    }
}
