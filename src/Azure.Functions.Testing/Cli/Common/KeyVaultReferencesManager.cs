using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Colors.Net;
using static Azure.Functions.Testing.Cli.Common.OutputTheme;

namespace Azure.Functions.Testing.Cli.Common
{
    internal class KeyVaultReferencesManager
    {
        private const string VaultUriSuffix = "vault.azure.net";
        private static readonly Regex BasicKeyVaultReferenceRegex = new(@"^@Microsoft\.KeyVault\((?<ReferenceString>.*)\)$", RegexOptions.Compiled);
        private readonly ConcurrentDictionary<string, SecretClient> _clients = new();
        private readonly TokenCredential _credential = new DefaultAzureCredential();

        public void ResolveKeyVaultReferences(IDictionary<string, string?> settings)
        {
            foreach (var key in settings.Keys.ToList())
            {
                try
                {
                    var keyVaultValue = GetSecretValue(key, settings[key]);
                    if (keyVaultValue != null)
                    {
                        settings[key] = keyVaultValue;
                    }
                }
                catch
                {
                    // Do not block StartHostAction if secret cannot be resolved: instead, skip it
                    // and attempt to resolve other secrets
                }
            }
        }

        private string? GetSecretValue(string key, string? value)
        {
            var result = ParseSecret(key, value);

            if (result != null)
            {
                var client = GetSecretClient(result.Uri);
                var secret = client.GetSecret(result.Name, result.Version);
                return secret.Value.Value;
            }

            return null;
        }

        internal ParseSecretResult? ParseSecret(string key, string? value)
        {
            // If the value is null, then we return nothing, as the subsequent call to
            // UpdateEnvironmentVariables(settings) will log to the user that the setting
            // is skipped. We check here, because Regex.Match throws when supplied with a
            // null value.
            if (value == null)
            {
                return null;
            }
            // Determine if the secret value is attempting to use a key vault reference
            var keyVaultReferenceMatch = BasicKeyVaultReferenceRegex.Match(value);
            if (keyVaultReferenceMatch.Success)
            {
                var referenceString = keyVaultReferenceMatch.Groups["ReferenceString"].Value;
                ParseSecretResult? result = null;
                try
                {
                    result = ParseVaultReference(referenceString);
                }
                catch
                {
                    // ignore and show warning below
                }

                // If we detect that a key vault reference was attempted, but did not match any of
                // the supported formats, we write a warning to the console.
                if (result == null)
                {
                    ColoredConsole.WriteLine(WarningColor($"Unable to parse the Key Vault reference for setting: {key}"));
                }
                return result;
            }
            return null;
        }

        internal ParseSecretResult? ParseVaultReference(string vaultReference)
        {
            var secretUriString = GetValueFromVaultReference("SecretUri", vaultReference);
            if (!string.IsNullOrEmpty(secretUriString))
            {
                var secretUri = new Uri(secretUriString);
                var secretIdent = new KeyVaultSecretIdentifier(secretUri);
                return new ParseSecretResult(secretIdent.VaultUri, secretIdent.Name, secretIdent.Version);
            }

            var vaultName = GetValueFromVaultReference("VaultName", vaultReference);
            var secretName = GetValueFromVaultReference("SecretName", vaultReference);
            var version = GetValueFromVaultReference("SecretVersion", vaultReference);
            if (!string.IsNullOrEmpty(vaultName) && !string.IsNullOrEmpty(secretName))
            {
                return new ParseSecretResult(new Uri($"https://{vaultName}.{VaultUriSuffix}"), secretName, version);
            }

            return null;
        }

        internal string? GetValueFromVaultReference(string key, string vaultReference)
        {
            var regex = new Regex(key + "=(?<Value>[^;]+)(;|$)");
            var match = regex.Match(vaultReference);
            if (match.Success)
            {
                return match.Groups["Value"].Value;
            }
            return null;
        }

        private SecretClient GetSecretClient(Uri vaultUri)
        {
            return _clients.GetOrAdd(vaultUri.ToString(), _ => new SecretClient(vaultUri, _credential));
        }

        internal class ParseSecretResult
        {
            public ParseSecretResult(Uri uri, string name, string? version)
            {
                Uri = uri;
                Name = name;
                Version = version;
            }

            public Uri Uri { get; }
            public string Name { get; }
            public string? Version { get; }
        }
    }
}
