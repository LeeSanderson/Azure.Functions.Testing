using Azure.Functions.Testing.Cli.Common;
using Azure.Functions.Testing.Cli.Helpers;
using Azure.Functions.Testing.Cli.Interfaces;

namespace Azure.Functions.Testing.Cli
{
    /// <summary>
    /// Initialize environment in the same way that Azure.Functions.Cli.Program
    /// </summary>
    internal class CliInitialization
    {
        public ISecretsManager SecretManager { get; }

        public CliInitialization()
        {
            SecretManager = new SecretsManager();
        }

        public void Init(string[] args)
        {
            SetCoreToolsEnvironmentVariables(args);
            // ConsoleApp.Run<Program>(args);
            UpdateCurrentDirectory(args);
            GlobalCoreToolsSettings.Init(SecretManager, args);
        }

        private static void SetCoreToolsEnvironmentVariables(string[] args)
        {
            EnvironmentHelper.SetEnvironmentVariableAsBoolIfNotExists(Constants.FunctionsCoreToolsEnvironment);
            EnvironmentHelper.SetEnvironmentVariableAsBoolIfNotExists(Constants.SequentialJobHostRestart);
            if (args.Contains("--debug", StringComparer.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable(Constants.CliDebug, "1");
            }
        }

        /// <summary>
        /// This method will update Environment.CurrentDirectory
        /// if there is a --script-root or a --prefix provided on the commandline
        /// </summary>
        /// <param name="args">args to check for --prefix or --script-root</param>
        private void UpdateCurrentDirectory(string[] args)
        {
            // assume index of -1 means the string is not there
            int index = -1;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--script-root", StringComparison.OrdinalIgnoreCase)
                    || args[i].Equals("--prefix", StringComparison.OrdinalIgnoreCase))
                {
                    // update the index to point to the following entry in args
                    // which should contain the path for a prefix
                    index = i + 1;
                    // _telemetryEvent.PrefixOrScriptRoot = true;
                    break;
                }
            }

            // make sure index still in the array
            if (index != -1 && index < args.Length)
            {
                // Path.Combine takes care of checking if the path is full path or not.
                // For example, Path.Combine(@"C:\temp", @"dir\dir")    => "C:\temp\dir\dir"
                //              Path.Combine(@"C:\temp", @"C:\Windows") => "C:\Windows"
                //              Path.Combine("/usr/bin", "dir/dir")     => "/usr/bin/dir/dir"
                //              Path.Combine("/usr/bin", "/opt/dir")    => "/opt/dir"
                var path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, args[index]));
                if (FileSystemHelpers.DirectoryExists(path))
                {
                    Environment.CurrentDirectory = path;
                }
                else
                {
                    throw new CliException($"\"{path}\" doesn't exist.");
                }
            }
        }

    }
}
