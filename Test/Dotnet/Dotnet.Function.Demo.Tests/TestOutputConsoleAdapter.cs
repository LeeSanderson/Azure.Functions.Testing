using System.Text;
using Xunit.Abstractions;

namespace Dotnet.Function.Demo.Tests;


internal class TestOutputConsoleAdapter : TextWriter
{
    private readonly ITestOutputHelper _output;

    public TestOutputConsoleAdapter(ITestOutputHelper output)
    {
        this._output = output;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? message)
    {
        _output.WriteLine(message);
    }
    public override void WriteLine(string format, params object?[] args)
    {
        _output.WriteLine(format, args);
    }

    public override void Write(char value)
    {
        throw new NotSupportedException("This text writer only supports WriteLine(string) and WriteLine(string, params object[]).");
    }

    public override void Write(string? message)
    {
        if (message?.Trim() == Environment.NewLine || string.IsNullOrEmpty(message))
        {
            // Skip new lines?
            return;
        }

        _output.WriteLine(message);
    }
}