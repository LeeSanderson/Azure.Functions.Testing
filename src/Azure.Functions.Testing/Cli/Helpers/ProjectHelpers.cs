using System.Xml;
using Azure.Functions.Testing.Cli.Common;
using Azure.Functions.Testing.Cli.Diagnostics;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

namespace Azure.Functions.Testing.Cli.Helpers
{
    internal static class ProjectHelpers
    {
        public static string? GetUserSecretsId(string scriptPath, LoggingFilterHelper loggingFilterHelper, LoggerFilterOptions loggerFilterOptions)
        {
            if (string.IsNullOrEmpty(scriptPath))
            {
                return null;
            }
            var projectFilePath = FindProjectFile(scriptPath, loggingFilterHelper, loggerFilterOptions);
            if (projectFilePath == null)
            {
                return null;
            }

            var projectRoot = GetProject(projectFilePath);
            if (projectRoot != null)
            {
                return GetPropertyValue(projectRoot, Constants.UserSecretsIdElementName);
            }

            return null;
        }

        public static string? FindProjectFile(string path, LoggingFilterHelper? loggingFilterHelper = null, LoggerFilterOptions? loggerFilterOptions = null)
        {
            ColoredConsoleLogger? logger = null;
            if (loggingFilterHelper != null && loggerFilterOptions != null)
            {
                logger = new ColoredConsoleLogger("ProjectHelpers", loggingFilterHelper, loggerFilterOptions);
            }
            bool shouldLog = logger != null;

            DirectoryInfo filePath = new DirectoryInfo(path);
            do
            {
                var projectFiles = filePath.GetFiles("*.csproj");
                if (projectFiles.Any())
                {
                    foreach (FileInfo file in projectFiles)
                    {
                        if (string.Equals(file.Name, Constants.ExtensionsCsProjFile, StringComparison.OrdinalIgnoreCase)) continue;
                        if (shouldLog)
                        {
                            logger!.LogDebug($"Found {file.FullName}. Using for user secrets file configuration.");
                        }
                        return file.FullName;
                    }
                }
                filePath = filePath.Parent!;
            }
            while (filePath.FullName != filePath.Root.FullName);

            if (shouldLog)
            {
                logger!.LogDebug($"Csproj not found in {path} directory tree. Skipping user secrets file configuration.");
            }
            return null;
        }

        public static ProjectRootElement? GetProject(string path)
        {
            ProjectRootElement? root = null;

            if (File.Exists(path))
            {
                var reader = XmlReader.Create(new StringReader(File.ReadAllText(path)));
                root = ProjectRootElement.Create(reader);
            }

            return root;
        }

        public static bool PackageReferenceExists(this ProjectRootElement project, string packageId)
        {
            var existingPackageReference = 
                project.Items
                .FirstOrDefault(item => item.ItemType == Constants.PackageReferenceElementName && item.Include.ToLowerInvariant() == packageId.ToLowerInvariant());
            return existingPackageReference != null;
        }

        public static string? GetPropertyValue(this ProjectRootElement project, string propertyName)
        {
            var property = project.Properties
                .FirstOrDefault(item => string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            return property?.Value;
        }
    }
}
