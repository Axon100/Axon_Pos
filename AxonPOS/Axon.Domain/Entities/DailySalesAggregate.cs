using System;

namespace Axon.Domain.Entities
{
    public class DailySalesAggregate
    {
        public DateTime Date { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalTax { get; set; }
        public int TransactionCount { get; set; }
    }
}
