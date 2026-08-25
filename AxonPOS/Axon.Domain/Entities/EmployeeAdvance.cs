using Axon.Domain.Common;
using System;
using System.Collections.Generic;

namespace Axon.Domain.Entities
{
    public class EmployeeAdvance : BaseEntity
    {
        public int EmployeeId { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime AdvanceDate { get; set; } = DateTime.Today;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = "غير مسددة"; // غير مسددة / سداد جزئي / مسددة بالكامل

        public virtual Employee Employee { get; set; } = null!;
        public virtual ICollection<EmployeeAdvancePayment> Payments { get; set; } = new List<EmployeeAdvancePayment>();
    }
}
