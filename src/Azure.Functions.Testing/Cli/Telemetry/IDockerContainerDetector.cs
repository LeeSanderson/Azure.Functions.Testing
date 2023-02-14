
namespace Azure.Functions.Testing.Cli.Telemetry;

internal interface IDockerContainerDetector
{
    DockerContainer IsDockerContainer();
}
