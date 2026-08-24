using Axon.Domain.Common;
using System;

namespace Axon.Domain.Entities
{
    public class Expense : BaseEntity
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTimeOffset ExpenseDate { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int UserId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
