using System.Text.RegularExpressions;

namespace IikoFront.OrderQrPlugin.Payload
{
    public static class PayloadEscaper
    {
        private static readonly Regex SpacesRegex = new Regex(@"\s+", RegexOptions.Compiled);

        public static string EscapeOrDash(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            var sanitized = value
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(":", "\\:")
                .Replace("[", "\\[")
                .Replace("]", "\\]")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");

            sanitized = SpacesRegex.Replace(sanitized, " ").Trim();
            return string.IsNullOrWhiteSpace(sanitized) ? "-" : sanitized;
        }
    }
}
