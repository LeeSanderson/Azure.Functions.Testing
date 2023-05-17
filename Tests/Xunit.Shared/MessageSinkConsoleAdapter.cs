using System.IO;
using System.Text;
using System;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit.Shared;

public class MessageSinkConsoleAdapter : TextWriter
{
    private IMessageSink? _diagnosticMessageSink;
    private readonly TextWriter _oldOut;
    private readonly TextWriter _oldError;

    public MessageSinkConsoleAdapter(IMessageSink diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
        _oldOut = Console.Out;
        _oldError = Console.Error;
        Console.SetOut(this);
        Console.SetError(this);
    }

    protected override void Dispose(bool disposing)
    {
        _diagnosticMessageSink = null;
        Console.SetOut(_oldOut);
        Console.SetError(_oldError);
        base.Dispose(disposing);
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? message) => 
        WriteMessageToSync(new DiagnosticMessage(message));

    public override void WriteLine(string format, params object?[] args) => 
        WriteMessageToSync(new DiagnosticMessage(format, args));

    public override void Write(char value) => 
        throw new NotSupportedException("This text writer only supports WriteLine(string) and WriteLine(string, params object[]).");

    private void WriteMessageToSync(DiagnosticMessage message) => 
        _diagnosticMessageSink?.OnMessage(message);
}