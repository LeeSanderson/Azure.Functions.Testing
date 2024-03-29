﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Azure.Functions.Testing;

internal static class CommandChecker
{
    public static bool CommandValid(string fileName, string args)
        => CheckExitCode(fileName, args);

    public static bool CommandExists(string command)
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? CheckExitCode("where", command)
            : CheckExitCode("/bin/bash", $"-c \"command -v {command}\"");

    public static async Task<bool> PowerShellModuleExistsAsync(string powerShellExecutable, string module)
    {
        // Attempt to get the specified module. If it cannot be found, throw.
        var exe = new Executable(powerShellExecutable,
            $"-NonInteractive -o Text -NoProfile -c if(!(Get-Module -ListAvailable {module})) {{ throw '{module} module not found' }}");

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var exitCode = await exe.RunAsync(o => stdout.AppendLine(o), e => stderr.AppendLine(e));
        return exitCode == 0;
    }

    private static bool CheckExitCode(string fileName, string args)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        var process = Process.Start(processStartInfo);
        process?.WaitForExit();
        return process?.ExitCode == 0;
    }
}