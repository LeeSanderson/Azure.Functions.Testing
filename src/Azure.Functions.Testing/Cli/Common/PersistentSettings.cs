using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure.Functions.Testing.Cli.Interfaces;

namespace Azure.Functions.Testing.Cli.Common
{
    internal class PersistentSettings : ISettings
    {
        private static readonly string PersistentSettingsPath =
            // ReSharper disable once StringLiteralTypo
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".azurefunctions", "config");

        private readonly DiskBacked<Dictionary<string, object>> _store;

        public PersistentSettings() : this(true)
        { }

        public PersistentSettings(bool global)
        {
            if (global)
            {
                FileSystemHelpers.EnsureDirectory(Path.GetDirectoryName(PersistentSettingsPath) ?? string.Empty);
                _store = DiskBacked.Create<Dictionary<string, object>>(PersistentSettingsPath);
            }
            else
            {
                _store = DiskBacked.Create<Dictionary<string, object>>(Path.Combine(Environment.CurrentDirectory, ".config"));
            }
        }

        private T GetConfig<T>(T @default, [CallerMemberName] string key = "")
        {
            if (_store.Value.ContainsKey(key))
            {
                return (T)_store.Value[key];
            }

            return @default;
        }

        private void SetConfig(object value, [CallerMemberName] string key = "")
        {
            _store.Value[key] = value;
            _store.Commit();
        }

        public Dictionary<string, object?> GetSettings()
        {
            return typeof(ISettings)
                .GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(this));
        }

        public void SetSetting(string name, string value)
        {
            _store.Value[name] = JsonConvert.DeserializeObject<JToken>(value)!;
            _store.Commit();
        }

        public bool DisplayLaunchingRunServerWarning { get { return GetConfig(true); } set { SetConfig(value); } }

        public bool RunFirstTimeCliExperience { get { return GetConfig(true); } set { SetConfig(value); } }

        public string CurrentSubscription { get { return GetConfig(string.Empty); } set { SetConfig(value); } }

        public string CurrentTenant { get { return GetConfig(string.Empty); } set { SetConfig(value); } }

        public string MachineId { get { return GetConfig(string.Empty); } set { SetConfig(value); } }

        public string IsDockerContainer { get { return GetConfig(string.Empty); } set { SetConfig(value); } }
    }
}
