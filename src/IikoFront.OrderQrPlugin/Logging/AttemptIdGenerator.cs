using System;
using System.Globalization;
using System.Threading;

namespace IikoFront.OrderQrPlugin.Logging
{
    public static class AttemptIdGenerator
    {
        private static int counter;

        public static string Next(string orderNumber)
        {
            var sequence = Interlocked.Increment(ref counter);
            var normalizedOrderNumber = string.IsNullOrWhiteSpace(orderNumber) ? "unknown" : orderNumber.Trim();
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}-{2:000}",
                DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                normalizedOrderNumber,
                sequence);
        }
    }
}
