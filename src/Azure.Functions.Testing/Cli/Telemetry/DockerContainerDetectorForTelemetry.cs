using System.Security;
using Microsoft.Win32;
using Microsoft.DotNet.PlatformAbstractions;

namespace Azure.Functions.Testing.Cli.Telemetry
{
    internal class DockerContainerDetectorForTelemetry : IDockerContainerDetector
    {
        public DockerContainer IsDockerContainer()
        {
            switch (RuntimeEnvironment.OperatingSystemPlatform)
            {
                case Platform.Windows:
                    try
                    {
                        #pragma warning disable CA1416
                        using RegistryKey? subKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control");
                        return subKey?.GetValue("ContainerType") != null
                            ? DockerContainer.True
                            : DockerContainer.False;
                        #pragma warning restore CA1416
                    }
                    catch (SecurityException)
                    {
                        return DockerContainer.Unknown;
                    }
                case Platform.Linux:
                    return ReadProcToDetectDockerInLinux()
                        ? DockerContainer.True
                        : DockerContainer.False;
                case Platform.Unknown:
                    return DockerContainer.Unknown;
                case Platform.Darwin:
                default:
                    return DockerContainer.False;
            }
        }

        private static bool ReadProcToDetectDockerInLinux()
        {
            return File
                // ReSharper disable once StringLiteralTypo
                .ReadAllText("/proc/1/cgroup")
                .Contains("/docker/");
        }
    }
}
