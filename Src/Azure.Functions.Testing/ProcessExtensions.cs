﻿using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Azure.Functions.Testing;

internal static class ProcessExtensions
{
    // http://stackoverflow.com/a/19104345
    public static Task<int> CreateWaitForExitTask(this Process process, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<int>();
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);
        if (cancellationToken != default)
        {
            cancellationToken.Register(tcs.SetCanceled);
        }

        return tcs.Task;
    }

    public static void KillProcessTree(this Process process)
    {
        foreach (var childProcess in process.GetChildren())
        {
            childProcess.Kill();
        }

        process.Kill();
    }

    // http://blogs.msdn.com/b/bclteam/archive/2006/06/20/640259.aspx
    // http://stackoverflow.com/questions/394816/how-to-get-parent-process-in-net-in-managed-way
    // https://github.com/projectkudu/kudu/blob/master/Kudu.Core/Infrastructure/ProcessExtensions.cs
    private static IEnumerable<Process> GetChildren(this Process process, bool recursive = true)
    {
        var pid = process.Id;
        var tree = GetProcessTree();
        return GetChildren(pid, tree, recursive).Select(SafeGetProcessById).Where(p => p != null).Select(p => p!);
    }

    private static Process? GetParentProcess(this Process process)
    {
        if (!OsDetector.IsOnWindows())
        {
            return process.GetParentProcessLinux();
        }

        if (!process.TryGetProcessHandle(out var processHandle))
        {
            return null;
        }

        var pbi = new ProcessNativeMethods.ProcessInformation();
        try
        {
            var status =
                ProcessNativeMethods.NtQueryInformationProcess(
                    processHandle,
                    0,
                    ref pbi,
                    Marshal.SizeOf(pbi),
                    out _);

            if (status != 0)
            {
                throw new Win32Exception(status);
            }

            return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
        }
        catch
        {
            return null;
        }
    }

    // http://stackoverflow.com/questions/2509406/c-mono-get-list-of-child-processes-on-windows-and-linux
    private static Process? GetParentProcessLinux(this Process process)
    {
        try
        {
            var procPath = "/proc/" + process.Id + "/stat";
            if (File.Exists(procPath))
            {
                var lines = File.ReadLines(procPath);
                var match = Regex.Match(lines.First(), @"\d+\s+\((.*?)\)\s+\w+\s+(\d+)\s");

                if (match.Success)
                {
                    var ppid = int.Parse(match.Groups[2].Value);
                    return ppid < 1 ? null : Process.GetProcessById(ppid);
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    // recursively get children.
    // return depth-first (leaf child first).
    private static IEnumerable<int> GetChildren(int pid, Dictionary<int, List<int>> tree, bool recursive)
    {
        if (tree.TryGetValue(pid, out var children))
        {
            var result = new List<int>();
            foreach (var id in children)
            {
                if (recursive)
                {
                    result.AddRange(GetChildren(id, tree, recursive));
                }
                result.Add(id);
            }
            return result;
        }
        return Enumerable.Empty<int>();
    }

    private static Dictionary<int, List<int>> GetProcessTree()
    {
        var tree = new Dictionary<int, List<int>>();
        foreach (var proc in Process.GetProcesses())
        {
            var parent = proc.GetParentProcess();
            if (parent != null)
            {
                if (!tree.TryGetValue(parent.Id, out var children))
                {
                    tree[parent.Id] = children = new List<int>();
                }

                children.Add(proc.Id);
            }
        }

        return tree;
    }

    private static bool TryGetProcessHandle(this Process process, out IntPtr processHandle)
    {
        try
        {
            processHandle = process.Handle;
        }
        catch (Win32Exception ex)
        {
            // process.Handle may fail due to access denied.
            // Handle the exception to reduce noises in trace errors.
            if (ex.NativeErrorCode != 5)
            {
                throw;
            }

            processHandle = IntPtr.Zero;
        }
        catch (InvalidOperationException)
        {
            // process.Handle may fail if the process has already exited.
            // Handle the exception to reduce noises in trace errors.
            processHandle = IntPtr.Zero;
        }

        return processHandle != IntPtr.Zero;
    }

    private static Process? SafeGetProcessById(int pid)
    {
        try
        {
            return Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            // Process with an Id is not running.
            return null;
        }
    }
}