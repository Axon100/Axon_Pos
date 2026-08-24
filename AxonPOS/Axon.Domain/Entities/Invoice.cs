using Axon.Domain.Common;
using System;

namespace Axon.Domain.Entities
{
    public class Invoice : BaseEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public int SaleId { get; set; }
        public DateTimeOffset IssueDate { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Status { get; set; } = "Issued"; // Issued, Paid, Cancelled, Refunded

        // Navigation properties
        public virtual Sale Sale { get; set; } = null!;
    }
}
