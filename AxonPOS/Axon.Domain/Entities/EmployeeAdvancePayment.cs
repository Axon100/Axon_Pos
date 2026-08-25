using Axon.Domain.Common;
using System;

namespace Axon.Domain.Entities
{
    public class EmployeeAdvancePayment : BaseEntity
    {
        public int EmployeeAdvanceId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Today;
        public decimal AmountPaid { get; set; }
        public string Notes { get; set; } = string.Empty;

        public virtual EmployeeAdvance EmployeeAdvance { get; set; } = null!;
    }
}
