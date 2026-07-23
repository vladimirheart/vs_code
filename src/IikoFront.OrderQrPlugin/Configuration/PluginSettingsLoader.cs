using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Resto.Front.Api;

namespace IikoFront.OrderQrPlugin.Configuration
{
    public sealed class PluginSettingsLoader
    {
        private const string FileName = "order-qr-settings.json";
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        private readonly ILog log;
        private readonly IPluginIntegrationService integrationService;

        public PluginSettingsLoader(ILog log, IPluginIntegrationService integrationService)
        {
            this.log = log;
            this.integrationService = integrationService;
        }

        public PluginSettings Load()
        {
            var settingsPath = Path.Combine(integrationService.GetConfigsDirectoryPath(), FileName);
            var defaults = PluginSettings.CreateDefault().Normalize();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath) ?? ".");

                if (!File.Exists(settingsPath))
                {
                    Save(settingsPath, defaults);
                    log.Info($"event=SETTINGS_CREATED path={escape(settingsPath)}");
                    return defaults;
                }

                using (var stream = File.OpenRead(settingsPath))
                {
                    var serializer = createSerializer();
                    var loaded = serializer.ReadObject(stream) as PluginSettings;
                    if (loaded == null)
                    {
                        log.Warn($"event=SETTINGS_DEFAULTS_USED reason=DESERIALIZED_NULL path={escape(settingsPath)}");
                        return defaults;
                    }

                    log.Info($"event=SETTINGS_LOADED path={escape(settingsPath)}");
                    return loaded.Normalize();
                }
            }
            catch (Exception ex)
            {
                log.Error($"event=SETTINGS_LOAD_FAILED path={escape(settingsPath)}", ex);
                return defaults;
            }
        }

        private static void Save(string path, PluginSettings settings)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = createSerializer();
                serializer.WriteObject(stream, settings);
                File.WriteAllText(path, Encoding.UTF8.GetString(stream.ToArray()), Utf8WithoutBom);
            }
        }

        private static DataContractJsonSerializer createSerializer()
        {
            return new DataContractJsonSerializer(typeof(PluginSettings));
        }

        private static string escape(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Replace(' ', '_');
        }
    }
}
