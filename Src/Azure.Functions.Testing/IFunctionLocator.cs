namespace Azure.Functions.Testing;

public interface IFunctionLocator
{
    string StartupDirectory { get; }
}

public static class FunctionLocator
{
    public static IFunctionLocator FromProject(string funcProjectFolder)
    {
        return new ProjectFunctionLocator(funcProjectFolder);
    }

    public static IFunctionLocator FromPath(string path)
    {
        return new PathFunctionLocator(path);
    }
}