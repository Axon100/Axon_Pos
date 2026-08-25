using Axon.Domain.Common;
using System;

namespace Axon.Domain.Entities
{
    public class EmployeeDeduction : BaseEntity
    {
        public int EmployeeId { get; set; }
        public DateTime DeductionDate { get; set; } = DateTime.Today;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public virtual Employee Employee { get; set; } = null!;
    }
}
