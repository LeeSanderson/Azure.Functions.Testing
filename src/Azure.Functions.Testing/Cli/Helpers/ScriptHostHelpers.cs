using Azure.Functions.Testing.Cli.Common;
using Microsoft.Azure.WebJobs.Script;

namespace Azure.Functions.Testing.Cli.Helpers
{
    public static class ScriptHostHelpers
    {
        private static bool _isHelpRunning;

        public static void SetIsHelpRunning()
        {
            _isHelpRunning = true;
        }

        public static string GetFunctionAppRootDirectory(string startingDirectory, List<string>? searchFiles = null)
        {
            if (_isHelpRunning)
            {
                return startingDirectory;
            }

            searchFiles ??= new List<string> { ScriptConstants.HostMetadataFileName };

            if (searchFiles.Any(file => FileSystemHelpers.FileExists(Path.Combine(startingDirectory, file))))
            {
                return startingDirectory;
            }

            var parent = Path.GetDirectoryName(startingDirectory);

            if (parent == null)
            {
                var files = searchFiles.Aggregate((acc, file) => $"{acc}, {file}");
                throw new CliException($"Unable to find project root. Expecting to find one of {files} in project root.");
            }
            else
            {
                return GetFunctionAppRootDirectory(parent, searchFiles);
            }
        }
    }
}
