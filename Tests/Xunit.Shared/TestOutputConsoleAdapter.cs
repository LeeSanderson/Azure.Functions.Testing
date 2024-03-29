﻿using System;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace Xunit.Shared;

public class TestOutputConsoleAdapter : TextWriter
{
    private ITestOutputHelper? _output;
    private readonly TextWriter _oldOut;
    private readonly TextWriter _oldError;

    public TestOutputConsoleAdapter(ITestOutputHelper output)
    {
        _output = output;
        _oldOut = Console.Out;
        _oldError = Console.Error;
        Console.SetOut(this);
        Console.SetError(this);
    }

    protected override void Dispose(bool disposing)
    {
        _output = null;
        Console.SetOut(_oldOut);
        Console.SetError(_oldError);
        base.Dispose(disposing);
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? message) => _output?.WriteLine(message ?? string.Empty);

    public override void WriteLine(string format, params object?[] args) => _output?.WriteLine(format, args);

    public override void Write(char value) => 
        throw new NotSupportedException("This text writer only supports WriteLine(string) and WriteLine(string, params object[]).");
}