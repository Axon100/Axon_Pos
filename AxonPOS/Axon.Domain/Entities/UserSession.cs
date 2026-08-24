using Axon.Domain.Common;
using System;

namespace Axon.Domain.Entities
{
    public class UserSession : BaseEntity
    {
        public int UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        
        public User? User { get; set; }
    }
}
