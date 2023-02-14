namespace Azure.Functions.Testing.Cli.Common
{
    public class PythonPackage
    {
        public PythonPackage(string name, string specification, string environmentMarkers, string directReference)
        {
            Name = name;
            Specification = specification;
            EnvironmentMarkers = environmentMarkers;
            DirectReference = directReference;
        }

        // azure-functions-worker
        public string Name { get; }

        // >=1.0.0,<1.0.3
        public string Specification { get; }

        // python_version < '2.8' or python_version == '2.7'
        public string EnvironmentMarkers { get; }

        // @ file:///somewhere
        public string DirectReference { get; }
    }
}
