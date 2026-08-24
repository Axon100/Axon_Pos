using Axon.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axon.Domain.Entities
{
    public class Sale : BaseEntity
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total => SubTotal + TaxAmount - DiscountAmount;
        
        public string Status { get; set; } = "New"; // New, Suspended, Completed, Refunded
        
        public int CashierId { get; set; }
        public User? Cashier { get; set; }

        public ICollection<SaleLineItem> LineItems { get; set; } = new List<SaleLineItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Return> Returns { get; set; } = new List<Return>();
        public Invoice? Invoice { get; set; }
        
        public void CalculateTotals()
        {
            SubTotal = LineItems.Sum(x => x.LineTotal);
            // Business Rule: For enterprise POS, Tax could be calculated per item based on Category Tax Profiles.
            // Simplified sum here.
        }
    }
}
