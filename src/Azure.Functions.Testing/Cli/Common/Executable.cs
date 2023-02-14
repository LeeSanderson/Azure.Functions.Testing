using System.ComponentModel;
using System.Diagnostics;
using Azure.Functions.Testing.Cli.Extensions;
using static Azure.Functions.Testing.Cli.Common.OutputTheme;

namespace Azure.Functions.Testing.Cli.Common
{
    internal class Executable
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
            if (StaticSettings.IsDebug)
            {
                Colors.Net.ColoredConsole.WriteLine(VerboseColor($"> {Command}"));
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

            var exitCodeTask = Process.CreateWaitForExitTask();

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
                    await Process.StandardInput.WriteLineAsync(stdIn);
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

            if (timeout == null)
            {
                return await exitCodeTask;
            }

            await Task.WhenAny(exitCodeTask, Task.Delay(timeout.Value));
            if (exitCodeTask.IsCompleted)
            {
                return exitCodeTask.Result;
            }

            Process.Kill();
            throw new Exception("Process didn't exit within specified timeout");
        }
    }
}
