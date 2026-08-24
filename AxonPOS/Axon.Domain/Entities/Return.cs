using Axon.Domain.Common;
using System;
using System.Collections.Generic;

namespace Axon.Domain.Entities
{
    public class Return : BaseEntity
    {
        public int SaleId { get; set; }
        public int UserId { get; set; }
        public DateTimeOffset ReturnDate { get; set; }
        public decimal TotalRefundAmount { get; set; }
        public string Reason { get; set; } = string.Empty;

        // Navigation properties
        public virtual Sale Sale { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<ReturnLineItem> ReturnLineItems { get; set; } = new List<ReturnLineItem>();
    }

    public class ReturnLineItem : BaseEntity
    {
        public int ReturnId { get; set; }
        public int SaleLineItemId { get; set; }
        public int QuantityReturned { get; set; }
        public decimal RefundAmount { get; set; }
        public bool RestockToInventory { get; set; }

        // Navigation properties
        public virtual Return Return { get; set; } = null!;
        public virtual SaleLineItem SaleLineItem { get; set; } = null!;
    }
}
