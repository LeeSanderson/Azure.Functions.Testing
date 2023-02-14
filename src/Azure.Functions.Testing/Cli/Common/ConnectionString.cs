namespace Azure.Functions.Testing.Cli.Common;

public class ConnectionString
{
    public ConnectionString(string value, string name, string providerName)
    {
        Value = value;
        Name = name;
        ProviderName = providerName;
    }

    public string Value { get; }

    public string Name { get; }

    public string ProviderName { get; }
}
