using System.Globalization;

namespace IikoFront.OrderQrPlugin.Payload
{
    public static class NumberFormatter
    {
        public static string Format(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
