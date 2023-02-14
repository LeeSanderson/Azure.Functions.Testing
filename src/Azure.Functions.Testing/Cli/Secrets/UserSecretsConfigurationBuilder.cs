using Microsoft.Azure.WebJobs.Script;
using Microsoft.Extensions.Configuration;

namespace Azure.Functions.Testing.Cli.Secrets;

internal class UserSecretsConfigurationBuilder : IConfigureBuilder<IConfigurationBuilder>
{
    private readonly string? _userSecretsId;

    public UserSecretsConfigurationBuilder(string? userSecretsId)
    {
        _userSecretsId = userSecretsId;
    }

    public void Configure(IConfigurationBuilder builder)
    {
        if (_userSecretsId == null)
        {
            return;
        }
        builder.AddUserSecrets(_userSecretsId);
    }
}
