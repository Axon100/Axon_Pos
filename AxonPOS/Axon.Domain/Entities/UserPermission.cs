using Axon.Domain.Common;

namespace Axon.Domain.Entities
{
    public class UserPermission : BaseEntity
    {
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public int PermissionId { get; set; }
        public virtual Permission Permission { get; set; } = null!;

        public bool IsGranted { get; set; } = true;
    }
}
