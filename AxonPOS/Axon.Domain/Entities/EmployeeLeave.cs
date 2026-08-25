using Axon.Domain.Common;
using System;

namespace Axon.Domain.Entities
{
    public class EmployeeLeave : BaseEntity
    {
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
        public string LeaveType { get; set; } = "إجازة إعتيادية"; // إجازة إعتيادية / إجازة مرضية / عارضة / غياب
        public int TotalDays { get; set; } = 1;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "مقبولة"; // مقبولة / قيد الانتظار / مرفوضة

        public virtual Employee Employee { get; set; } = null!;
    }
}
