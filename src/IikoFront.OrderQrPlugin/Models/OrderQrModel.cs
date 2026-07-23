using System;
using System.Collections.Generic;

namespace IikoFront.OrderQrPlugin.Models
{
    public sealed class OrderQrModel
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = "-";
        public DateTime? PrintTime { get; set; }
        public int RepeatBillNumber { get; set; }
        public int ActiveItemCount { get; set; }
        public List<OrderItemQrModel> Items { get; } = new List<OrderItemQrModel>();
        public int TotalModifierCount { get; set; }
    }
}
