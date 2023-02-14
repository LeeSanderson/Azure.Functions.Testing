using System.Reflection;

namespace Azure.Functions.Testing;

internal class FunctionHostFactoryResolver
{
    private const BindingFlags DeclaredOnlyLookup = 
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    public const string CreateHostBuilder = nameof(CreateHostBuilder);

    public static Func<string[], THostBuilder>? ResolveHostBuilderFactory<THostBuilder>(Assembly assembly)
    {
        return ResolveFactory<THostBuilder>(assembly, CreateHostBuilder);
    }

    private static Func<string[], T>? ResolveFactory<T>(Assembly assembly, string name)
    {
        var programType = assembly?.EntryPoint?.DeclaringType;
        if (programType == null)
        {
            return null;
        }

        var factory = programType.GetMethod(name, DeclaredOnlyLookup);
        if (!IsFactory<T>(factory))
        {
            return null;
        }

        return args => (T)factory!.Invoke(null, new object[] { args })!;
    }

    // TReturn Factory(string[] args);
    private static bool IsFactory<TReturn>(MethodInfo? factory)
    {
        return factory != null
               && typeof(TReturn).IsAssignableFrom(factory.ReturnType)
               && factory.GetParameters().Length == 1
               && typeof(string[]) == factory.GetParameters()[0].ParameterType;
    }

}
