using System.Linq;
using System.Text;
using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Printing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resto.Front.Api.Data.Cheques;

namespace IikoFront.OrderQrPlugin.Tests
{
    [TestClass]
    public class QrMarkupFactoryTests
    {
        [TestMethod]
        public void Create_WhenUtf8ViaCp866Enabled_ProducesUtf8BytesInsideQr()
        {
            var factory = new QrMarkupFactory();
            var settings = new PluginSettings
            {
                QrPayloadEncodingMode = "Utf8ViaPrinterCodePage",
                QrPayloadPrinterCodePage = 866
            };
            const string payload = "IIKOQR1\n1;N:Ролл Креветка";

            var extensions = factory.Create(payload, "test-001", settings);
            var qrElement = extensions.AfterFooter.Descendants(Tags.QRCode).Single();
            var transportText = qrElement.Value;
            var restoredPayload = Encoding.UTF8.GetString(Encoding.GetEncoding(866).GetBytes(transportText));

            Assert.AreEqual(payload, restoredPayload);
        }

        [TestMethod]
        public void Create_WhenRawModeEnabled_LeavesPayloadUnchanged()
        {
            var factory = new QrMarkupFactory();
            var settings = new PluginSettings
            {
                QrPayloadEncodingMode = "Raw",
                QrPayloadPrinterCodePage = 866
            };
            const string payload = "IIKOQR1\n1;N:Ролл Креветка";

            var extensions = factory.Create(payload, "test-002", settings);
            var qrElement = extensions.AfterFooter.Descendants(Tags.QRCode).Single();

            Assert.AreEqual(payload, qrElement.Value);
        }
    }
}
