using Axon.Domain.Common;

namespace Axon.Domain.Entities
{
    public class SystemSetting : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty; // e.g., "StoreProfile", "Localization", "Integrations"
        public string Description { get; set; } = string.Empty;
    }
}
