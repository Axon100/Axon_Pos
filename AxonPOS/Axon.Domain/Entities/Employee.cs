using Axon.Domain.Common;
using System;
using System.Collections.Generic;

namespace Axon.Domain.Entities
{
    public class Employee : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public DateTime HireDate { get; set; } = DateTime.Today;
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }

        public virtual User? User { get; set; }
        public virtual ICollection<EmployeeAdvance> Advances { get; set; } = new List<EmployeeAdvance>();
        public virtual ICollection<EmployeeSalaryPayment> SalaryPayments { get; set; } = new List<EmployeeSalaryPayment>();
        public virtual ICollection<EmployeeAttendance> Attendances { get; set; } = new List<EmployeeAttendance>();
        public virtual ICollection<EmployeeDeduction> Deductions { get; set; } = new List<EmployeeDeduction>();
        public virtual ICollection<EmployeeLeave> Leaves { get; set; } = new List<EmployeeLeave>();
    }
}
