namespace Azure.Functions.Testing;

public class FunctionApplicationFactoryException : Exception
{
    public FunctionApplicationFactoryException(string message): base(message)
    {
    }
}