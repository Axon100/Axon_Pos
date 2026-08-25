using Axon.Domain.Common;
using System;

namespace Axon.Domain.Entities
{
    public class EmployeeSalaryPayment : BaseEntity
    {
        public int EmployeeId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Today;
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal DeductionAmount { get; set; }
        public decimal AdvanceDeduction { get; set; }
        public decimal NetSalary { get; set; }
        public string Notes { get; set; } = string.Empty;

        public virtual Employee Employee { get; set; } = null!;
    }
}
