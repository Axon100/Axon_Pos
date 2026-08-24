using Axon.Domain.Common;
using System;
using System.Collections.Generic;

namespace Axon.Domain.Entities
{
    public class InventoryTransaction : BaseEntity
    {
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Type { get; set; } = "Stock In"; // Stock In, Stock Out, Adjustment, Sale, Return
        public DateTime Date { get; set; } = DateTime.Now;
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Notes { get; set; } = string.Empty;

        public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
    }
}
