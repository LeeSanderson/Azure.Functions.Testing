using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Functions.Testing.Cli.Common
{
    public class ProtectedData
    {
        private static readonly ServiceProvider Services;

        static ProtectedData()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            Services = serviceCollection.BuildServiceProvider();
        }

        public static byte[] Protect(byte[] data, string purpose)
        {
            var protector = Services.GetDataProtector(purpose);
            return protector.Protect(data);
        }

        public static byte[] Unprotect(byte[] data, string purpose)
        {
            var protector = Services.GetDataProtector(purpose);
            return protector.Unprotect(data);
        }
    }
}
