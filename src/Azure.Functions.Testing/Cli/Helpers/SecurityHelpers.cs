using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Colors.Net;
using System.Text;
using System.Security.Cryptography;
using Azure.Functions.Testing.Cli.Common;
using Azure.Functions.Testing.Cli.NativeMethods;
using static Colors.Net.StringStaticMethods;

namespace Azure.Functions.Testing.Cli.Helpers
{
    internal static class SecurityHelpers
    {
        public static string? ReadPassword()
        {
            var password = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ConsoleNativeMethods.ReadPassword()
                : InternalReadPassword();
            Console.WriteLine();
            return password;
        }

        // https://stackoverflow.com/q/3404421/3234163
        private static string InternalReadPassword()
        {
            var password = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password.Append(key.KeyChar);
                    ColoredConsole.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    ColoredConsole.Write("\b \b");
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    return password.ToString();
                }
            }
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        internal static async Task<X509Certificate2> GetOrCreateCertificate(string certPath, string certPassword)
        {
            if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(certPassword))
            {
                certPassword = File.Exists(certPassword)
                    ? (await File.ReadAllTextAsync(certPassword)).Trim()
                    : certPassword;
                return new X509Certificate2(certPath, certPassword);
            }

            if (CommandChecker.CommandExists("openssl"))
            {
                return await CreateCertificateOpenSsl();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ColoredConsole
                    .WriteLine("Auto cert generation is currently not working on the .NET Core build.")
                    .WriteLine("On Windows you can run:")
                    .WriteLine()
                    .Write(DarkCyan("PS> "))
                    .WriteLine($"$cert = {DarkYellow("New-SelfSignedCertificate")} -Subject localhost -DnsName localhost -FriendlyName \"Functions Development\" -KeyUsage DigitalSignature -TextExtension @(\"2.5.29.37={{text}}1.3.6.1.5.5.7.3.1\")")
                    .Write(DarkCyan("PS> "))
                    .WriteLine($"{DarkYellow("Export-PfxCertificate")} -Cert $cert -FilePath certificate.pfx -Password (ConvertTo-SecureString -String {Red("<password>")} -Force -AsPlainText)")
                    .WriteLine()
                    .WriteLine("For more checkout https://docs.microsoft.com/en-us/aspnet/core/security/https")
                    .WriteLine();
            }
            else
            {
                ColoredConsole
                    .WriteLine("Auto cert generation is currently not working on the .NET Core build.")
                    .WriteLine("On Unix you can run:")
                    .WriteLine()
                    .Write(DarkGreen("sh> "))
                    .WriteLine("openssl req -new -x509 -newkey rsa:2048 -keyout localhost.key -out localhost.cer -days 365 -subj /CN=localhost")
                    .Write(DarkGreen("sh> "))
                    .WriteLine("openssl pkcs12 -export -out certificate.pfx -inkey localhost.key -in localhost.cer")
                    .WriteLine()
                    .WriteLine("For more checkout https://docs.microsoft.com/en-us/aspnet/core/security/https")
                    .WriteLine();
            }

            throw new CliException("Auto cert generation is currently not working on the .NET Core build.");
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        internal static async Task<X509Certificate2> CreateCertificateOpenSsl()
        {
            const string defaultPassword = "localcert";

            var certFileNames = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", String.Empty));
            var output = new StringBuilder();

            ColoredConsole.WriteLine("Generating a self signed certificate using openssl");
            var opensslKey = new Executable("openssl", $"req -new -x509 -newkey rsa:2048 -nodes -keyout {certFileNames}localhost.key -out {certFileNames}localhost.cer -days 365 -subj /CN=localhost");
            var exitCode = await opensslKey.RunAsync(o => output.AppendLine(o), e => output.AppendLine(e));
            if (exitCode != 0)
            {
                ColoredConsole.Error.WriteLine(output.ToString());
                throw new CliException("Could not create a key pair required for an openssl certificate.");
            }

            Executable opensslCert = new Executable("openssl", $"pkcs12 -export -out {certFileNames}certificate.pfx -inkey {certFileNames}localhost.key -in {certFileNames}localhost.cer -passout pass:{defaultPassword}");
            exitCode = await opensslCert.RunAsync(o => output.AppendLine(o), e => output.AppendLine(e));
            if (exitCode != 0)
            {
                ColoredConsole.Error.WriteLine(output.ToString());
                throw new CliException("Could not create a Certificate using openssl.");
            }

            return new X509Certificate2($"{certFileNames}certificate.pfx", defaultPassword);
        }

        public static string CalculateMd5(Stream stream)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(stream);
            var base64String = Convert.ToBase64String(hash);
            stream.Position = 0;
            return base64String;
        }

        public static string CalculateMd5(string file)
        {
            using var stream = FileSystemHelpers.OpenFile(file, FileMode.Open);
            return CalculateMd5(stream);
        }
    }
}
