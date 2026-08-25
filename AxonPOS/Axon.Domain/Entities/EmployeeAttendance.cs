using Axon.Domain.Common;
using System;

namespace Axon.Domain.Entities
{
    public class EmployeeAttendance : BaseEntity
    {
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public string Status { get; set; } = "حاضر"; // حاضر / غائب / تأخير / انصراف مبكر
        public double WorkHours { get; set; }
        public string Notes { get; set; } = string.Empty;

        public virtual Employee Employee { get; set; } = null!;
    }
}
