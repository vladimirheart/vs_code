using IikoFront.OrderQrPlugin.Payload;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IikoFront.OrderQrPlugin.Tests
{
    [TestClass]
    public class PayloadEscaperTests
    {
        [TestMethod]
        public void EscapeOrDash_WhenValueNull_ReturnsDash()
        {
            Assert.AreEqual("-", PayloadEscaper.EscapeOrDash(null));
        }

        [TestMethod]
        public void EscapeOrDash_EscapesSeparators()
        {
            var escaped = PayloadEscaper.EscapeOrDash("Суп; острый: большой [спец]");

            Assert.AreEqual(@"Суп\; острый\: большой \[спец\]", escaped);
        }

        [TestMethod]
        public void EscapeOrDash_CollapsesWhitespace()
        {
            var escaped = PayloadEscaper.EscapeOrDash("Мисо\r\n\tсуп");

            Assert.AreEqual("Мисо суп", escaped);
        }
    }
}
