using Axon.Domain.Common;
using System;

namespace Axon.Domain.Entities
{
    public class AuditLog : BaseEntity
    {
        public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string OldValues { get; set; } = string.Empty; // JSON
        public string NewValues { get; set; } = string.Empty; // JSON
        public DateTimeOffset Timestamp { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
