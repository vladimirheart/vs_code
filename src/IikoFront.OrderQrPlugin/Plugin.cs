using System;
using System.Reflection;
using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Extraction;
using IikoFront.OrderQrPlugin.Logging;
using IikoFront.OrderQrPlugin.Payload;
using IikoFront.OrderQrPlugin.Printing;
using Resto.Front.Api;

namespace IikoFront.OrderQrPlugin
{
    public sealed class Plugin : IFrontPlugin
    {
        private readonly IDisposable subscription;
        private readonly PrintAttemptLogger attemptLogger;

        public Plugin()
        {
            try
            {
                var pluginVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
                var iikoVersion = PluginContext.Integration.GetApplicationVersion();
                var configDirectory = PluginContext.Integration.GetConfigsDirectoryPath();
                var dataDirectory = PluginContext.Integration.GetDataStorageDirectoryPath();

                PluginContext.Log.Info(
                    $"event=PLUGIN_STARTING pluginVersion={pluginVersion} iikoVersion={pluginVersionOrDash(iikoVersion)} configDir={logValue(configDirectory)} dataDir={logValue(dataDirectory)}");

                var settingsLoader = new PluginSettingsLoader(PluginContext.Log, PluginContext.Integration);
                var settings = settingsLoader.Load();

                attemptLogger = new PrintAttemptLogger(PluginContext.Log, PluginContext.Integration, settings);

                var foodValueExtractor = new FoodValueExtractor(settings);
                var productItemExtractor = new ProductItemExtractor(
                    PluginContext.Operations,
                    settings,
                    foodValueExtractor,
                    attemptLogger);
                var orderDataExtractor = new OrderDataExtractor(settings, productItemExtractor, attemptLogger);
                var payloadBuilder = new OrderPayloadBuilder();
                var qrMarkupFactory = new QrMarkupFactory();
                var extender = new BillChequeQrExtender(
                    PluginContext.Operations,
                    settings,
                    orderDataExtractor,
                    payloadBuilder,
                    qrMarkupFactory,
                    attemptLogger);

                subscription = PluginContext.Notifications.BillChequePrinting.Subscribe(extender.OnBillChequePrinting);

                PluginContext.Log.Info(
                    $"event=PLUGIN_STARTED pluginVersion={pluginVersion} iikoVersion={pluginVersionOrDash(iikoVersion)} subscribed=true");
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error("event=PLUGIN_START_FAILED", ex);
            }
        }

        public void Dispose()
        {
            try
            {
                subscription?.Dispose();
                attemptLogger?.FlushNotice();
                PluginContext.Log.Info("event=PLUGIN_STOPPED");
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error("event=PLUGIN_STOP_FAILED", ex);
            }
        }

        private static string pluginVersionOrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Replace(' ', '_');
        }

        private static string logValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Replace(' ', '_');
        }
    }
}
