using System.Runtime.Serialization;

namespace IikoFront.OrderQrPlugin.Configuration
{
    [DataContract]
    public sealed class PluginSettings
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "printOnGuestBill")]
        public bool PrintOnGuestBill { get; set; } = true;

        [DataMember(Name = "payloadVersion")]
        public string PayloadVersion { get; set; } = "IIKOQR1";

        [DataMember(Name = "qrSize")]
        public string QrSize { get; set; } = "Extralarge";

        [DataMember(Name = "qrCorrection")]
        public string QrCorrection { get; set; } = "Low";

        [DataMember(Name = "treatAllZeroFoodValueAsMissing")]
        public bool TreatAllZeroFoodValueAsMissing { get; set; } = true;

        [DataMember(Name = "maxPayloadUtf8BytesWarning")]
        public int MaxPayloadUtf8BytesWarning { get; set; } = 2500;

        [DataMember(Name = "writeFullPayloadToStandardLog")]
        public bool WriteFullPayloadToStandardLog { get; set; }

        [DataMember(Name = "writeJsonlAuditLog")]
        public bool WriteJsonlAuditLog { get; set; } = true;

        [DataMember(Name = "includeOrderGuidInPayload")]
        public bool IncludeOrderGuidInPayload { get; set; }

        [DataMember(Name = "includeModifiers")]
        public bool IncludeModifiers { get; set; } = true;

        [DataMember(Name = "includeAllergens")]
        public bool IncludeAllergens { get; set; } = true;

        [DataMember(Name = "includePrintTime")]
        public bool IncludePrintTime { get; set; } = true;

        [DataMember(Name = "printOnCookingStart")]
        public bool PrintOnCookingStart { get; set; } = true;

        [DataMember(Name = "printDeliveryBillOnCookingStart")]
        public bool PrintDeliveryBillOnCookingStart { get; set; } = true;

        [DataMember(Name = "printTableBillOnCookingStart")]
        public bool PrintTableBillOnCookingStart { get; set; } = true;

        [DataMember(Name = "cookingStartInitialDelayMs")]
        public int CookingStartInitialDelayMs { get; set; } = 5000;

        [DataMember(Name = "cookingStartRetryDelayMs")]
        public int CookingStartRetryDelayMs { get; set; } = 2000;

        [DataMember(Name = "cookingStartMaxAttempts")]
        public int CookingStartMaxAttempts { get; set; } = 30;

        public static PluginSettings CreateDefault()
        {
            return new PluginSettings();
        }

        public PluginSettings Normalize()
        {
            PayloadVersion = string.IsNullOrWhiteSpace(PayloadVersion) ? "IIKOQR1" : PayloadVersion.Trim();
            QrSize = string.IsNullOrWhiteSpace(QrSize) ? "Extralarge" : QrSize.Trim();
            QrCorrection = string.IsNullOrWhiteSpace(QrCorrection) ? "Low" : QrCorrection.Trim();

            if (MaxPayloadUtf8BytesWarning <= 0)
            {
                MaxPayloadUtf8BytesWarning = 2500;
            }

            if (CookingStartInitialDelayMs < 0)
            {
                CookingStartInitialDelayMs = 5000;
            }

            if (CookingStartRetryDelayMs <= 0)
            {
                CookingStartRetryDelayMs = 2000;
            }

            if (CookingStartMaxAttempts <= 0)
            {
                CookingStartMaxAttempts = 30;
            }

            return this;
        }
    }
}
