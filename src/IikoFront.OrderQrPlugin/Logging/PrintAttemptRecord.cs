using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace IikoFront.OrderQrPlugin.Logging
{
    [DataContract]
    public sealed class PrintAttemptRecord
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        public PrintAttemptRecord(Guid orderId)
        {
            Timestamp = DateTimeOffset.Now.ToString("o");
            OrderId = orderId.ToString("D");
        }

        [DataMember(Name = "timestamp")]
        public string Timestamp { get; set; }

        [DataMember(Name = "attemptId")]
        public string AttemptId { get; set; }

        [DataMember(Name = "orderId")]
        public string OrderId { get; set; }

        [DataMember(Name = "orderNumber")]
        public string OrderNumber { get; set; } = "-";

        [DataMember(Name = "repeatBillNumber")]
        public int RepeatBillNumber { get; set; }

        [DataMember(Name = "items")]
        public int Items { get; set; }

        [DataMember(Name = "modifierCount")]
        public int ModifierCount { get; set; }

        [DataMember(Name = "characters")]
        public int Characters { get; set; }

        [DataMember(Name = "utf8Bytes")]
        public int Utf8Bytes { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; } = "STARTED";

        [DataMember(Name = "payload")]
        public string Payload { get; set; } = string.Empty;

        [DataMember(Name = "errorType")]
        public string ErrorType { get; set; } = string.Empty;

        [DataMember(Name = "errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;

        [DataMember(Name = "elapsedMs")]
        public long ElapsedMs { get; set; }

        public void Complete(string status)
        {
            Status = status;
            stopwatch.Stop();
            ElapsedMs = stopwatch.ElapsedMilliseconds;
        }

        public void Complete()
        {
            stopwatch.Stop();
            ElapsedMs = stopwatch.ElapsedMilliseconds;
        }
    }
}
