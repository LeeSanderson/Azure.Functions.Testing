using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Text;
using Azure.Functions.Testing.Cli.Common;

namespace Azure.Functions.Testing.Cli.Telemetry;

internal static class MacAddressGetter
{
    private const string MacRegex = @"(?:[a-z0-9]{2}[:\-]){5}[a-z0-9]{2}";
    private const string ZeroRegex = @"(?:00[:\-]){5}00";
    private const int ErrorFileNotFound = 0x2;

    public static string? GetMacAddress()
    {
        try
        {
            var shellOutput = GetShellOutMacAddressOutput().GetAwaiter().GetResult();
            if (shellOutput == null)
            {
                return null;
            }

            return ParseMacAddress(shellOutput);
        }
        catch (Win32Exception e)
        {
            if (e.NativeErrorCode == ErrorFileNotFound)
            {
                return GetMacAddressByNetworkInterface();
            }
            else
            {
                throw;
            }
        }
    }

    private static string? ParseMacAddress(string shellOutput)
    {
        string? macAddress = null;
        foreach (Match match in Regex.Matches(shellOutput, MacRegex, RegexOptions.IgnoreCase))
        {
            if (!Regex.IsMatch(match.Value, ZeroRegex))
            {
                macAddress = match.Value;
                break;
            }
        }

        return macAddress;
    }

    private static async Task<string?> GetIpCommandOutput()
    {
        return await ExecuteAndOutput("ip", "link");
    }

    private static async Task<string?> GetShellOutMacAddressOutput()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // ReSharper disable once StringLiteralTypo
            return await ExecuteAndOutput("getmac.exe", null);
        }

        try
        {
            // ReSharper disable once StringLiteralTypo
            var ifConfigResult = await ExecuteAndOutput("ifconfig", "-a");

            if (!string.IsNullOrEmpty(ifConfigResult))
            {
                return ifConfigResult;
            }

            return await GetIpCommandOutput();
        }
        catch (Win32Exception e)
        {
            if (e.NativeErrorCode == ErrorFileNotFound)
            {
                return await GetIpCommandOutput();
            }

            throw;
        }
    }

    private static async Task<string?> ExecuteAndOutput(string command, string? args)
    {
        var exe = new Executable(command, args);
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var exitCode = await exe.RunAsync(o => stdout.AppendLine(o), e => stderr.AppendLine(e));
        if (exitCode == 0)
        {
            return stdout.ToString();
        }

        return null;
    }

    private static string? GetMacAddressByNetworkInterface()
    {
        return GetMacAddressesByNetworkInterface().FirstOrDefault();
    }

    private static IEnumerable<string> GetMacAddressesByNetworkInterface()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        var macs = new List<string>();

        if (networkInterfaces is not {Length: >= 1})
        {
            macs.Add(string.Empty);
            return macs;
        }

        foreach (NetworkInterface adapter in networkInterfaces)
        {
            PhysicalAddress address = adapter.GetPhysicalAddress();
            byte[] bytes = address.GetAddressBytes();
            macs.Add(string.Join("-", bytes.Select(x => x.ToString("X2"))));
            if (macs.Count >= 10)
            {
                break;
            }
        }
        return macs;
    }
}
