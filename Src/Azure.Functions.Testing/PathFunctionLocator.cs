namespace Azure.Functions.Testing;

internal class PathFunctionLocator : IFunctionLocator
{
    private readonly string _path;

    public PathFunctionLocator(string path)
    {
        _path = path;
        throw new NotImplementedException();
    }

    public string StartupDirectory
    {
        get
        {
            if (!Directory.Exists(_path))
            {
                throw new DirectoryNotFoundException($"Path {_path} does not exist");
            }

            return _path;
        }
    }
}