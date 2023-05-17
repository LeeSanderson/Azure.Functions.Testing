using System.ComponentModel;
using System.Diagnostics;

namespace Azure.Functions.Testing;

internal class Executable : IDisposable
{
    private readonly string? _arguments;
    private readonly string _exeName;
    private readonly bool _shareConsole;
    private readonly bool _streamOutput;
    private readonly bool _visibleProcess;
    private readonly string? _workingDirectory;

    public Executable(
        string exeName,
        string? arguments = null,
        bool streamOutput = true,
        bool shareConsole = false,
        bool visibleProcess = false,
        string? workingDirectory = null)
    {
        _exeName = exeName;
        _arguments = arguments;
        _streamOutput = streamOutput;
        _shareConsole = shareConsole;
        _visibleProcess = visibleProcess;
        _workingDirectory = workingDirectory;
    }

    public string Command => $"{_exeName} {_arguments}";

    public Process? Process { get; private set; }

    public async Task<int> RunAsync(
        Action<string?>? outputCallback = null,
        Action<string?>? errorCallback = null,
        TimeSpan? timeout = null,
        string? stdIn = null)
    {
        Start(outputCallback, errorCallback, stdIn);
        var exitCodeTask = Process!.CreateWaitForExitTask();
        if (timeout == null)
        {
            return await exitCodeTask;
        }

        await Task.WhenAny(exitCodeTask, Task.Delay(timeout.Value));
        if (exitCodeTask.IsCompleted)
        {
            return exitCodeTask.Result;
        }

        Process!.KillProcessTree();
        throw new Exception("Process didn't exit within specified timeout");
    }

    public async Task<(bool, int)> TryGetExitCode(TimeSpan timeout)
    {
        if (Process == null)
        {
            throw new Exception("Process is not running. Call Start or RunAsync first");
        }

        var exitCode = -1;
        var exitCodeTask = Process!.CreateWaitForExitTask();
        Process.KillProcessTree();

        await Task.WhenAny(exitCodeTask, Task.Delay(timeout));
        var processCompleted = exitCodeTask.IsCompleted;
        if (processCompleted)
        {
            exitCode = exitCodeTask.Result;
        }

        return (processCompleted, exitCode);
    }

    public void Start(
        Action<string?>? outputCallback = null,
        Action<string?>? errorCallback = null,
        string? stdIn = null)
    {
        if (StaticSettings.IsDebug)
        {
            Console.WriteLine($"> {Command}");
        }

        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _exeName,
                Arguments = _arguments,
                CreateNoWindow = !_visibleProcess,
                UseShellExecute = _shareConsole,
                RedirectStandardError = _streamOutput,
                RedirectStandardInput = _streamOutput || !string.IsNullOrEmpty(stdIn),
                RedirectStandardOutput = _streamOutput,
                WorkingDirectory = _workingDirectory ?? Environment.CurrentDirectory
            }
        };

        if (_streamOutput)
        {
            Process.OutputDataReceived += (_, e) =>
            {
                outputCallback?.Invoke(e.Data);
            };

            Process.ErrorDataReceived += (_, e) =>
            {
                errorCallback?.Invoke(e.Data);
            };
            Process.EnableRaisingEvents = true;
        }

        try
        {
            Process.Start();
            if (_streamOutput)
            {
                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();
            }

            if (!string.IsNullOrEmpty(stdIn))
            {
                Process.StandardInput.WriteLine(stdIn);
                Process.StandardInput.Close();
            }
        }
        catch (Win32Exception ex)
        {
            if (ex.Message == "The system cannot find the file specified")
            {
                throw new FileNotFoundException(ex.Message, ex);
            }
            throw;
        }
    }


    public void Dispose()
    {
        try
        {
            Process?.Kill();
        }
        catch
        {
            // Ignore errors on kill/dispose
        }

        Process?.Dispose();
    }
}