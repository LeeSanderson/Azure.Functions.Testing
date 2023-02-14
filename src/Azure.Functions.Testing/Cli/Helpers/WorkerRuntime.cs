// ReSharper disable InconsistentNaming
namespace Azure.Functions.Testing.Cli.Helpers;

public enum WorkerRuntime
{
    None,
    dotnet,
    dotnetIsolated,
    node,
    python,
    java,
    // ReSharper disable once IdentifierTypo
    powershell,
    custom
}
