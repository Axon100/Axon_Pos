using Axon.Domain.Common;
using System.Collections.Generic;

namespace Axon.Domain.Entities
{
    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }
}
