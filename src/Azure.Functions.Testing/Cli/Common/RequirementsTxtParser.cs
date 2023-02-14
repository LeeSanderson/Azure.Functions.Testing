using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Functions.Testing.Cli.Common
{
    // Requirements.txt PEP https://www.python.org/dev/peps/pep-0440/
    public static class RequirementsTxtParser
    {
        public static async Task<List<PythonPackage>> ParseRequirementsTxtFile(string functionAppRoot)
        {
            // Check if requirements.txt exist
            string requirementsTxtPath = Path.Join(functionAppRoot, Constants.RequirementsTxt);
            if (!FileSystemHelpers.FileExists(requirementsTxtPath))
            {
                return new List<PythonPackage>();
            }

            // Parse requirements.txt line by line
            string fileContent = await FileSystemHelpers.ReadAllTextFromFileAsync(requirementsTxtPath);
            return ParseRequirementsTxtContent(fileContent);
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public static List<PythonPackage> ParseRequirementsTxtContent(string fileContent)
        {
            const string pattern = @"^(?<name>(\w|\-|_|\.)+\s*)((?<spec>(===|==|<=|>=|!=|~=|>|<)[(\d|\w\d)\.]*[^;@])?)((;(?<envmarker>[^@]+))?)((@(?<directref>[^$]+))?)$";
            Regex rx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var packages = new ConcurrentBag<PythonPackage>();

            fileContent.Split('\r', '\n').Where(l => !string.IsNullOrWhiteSpace(l)).AsParallel().ForAll(line => {
                Match match = rx.Match(line);

                if (match.Success)
                {
                    GroupCollection groups = match.Groups;

                    groups.TryGetValue("name", out var packageName);
                    groups.TryGetValue("spec", out var packageSpec);
                    groups.TryGetValue("envmarker", out var packageEnvMarker);
                    groups.TryGetValue("directref", out var packageDirectRef);

                    if (packageName != null)
                    {
                        packages.Add(new PythonPackage(
                            packageName.Value.ToLower().Replace('_', '-').Replace('.', '-').Trim(),
                            packageSpec?.Value.Trim() ?? string.Empty,
                            packageEnvMarker?.Value.Trim() ?? string.Empty,
                            packageDirectRef?.Value.Trim() ?? string.Empty
                        ));
                    }
                }
            });

            return packages.ToList();
        }
    }
}
