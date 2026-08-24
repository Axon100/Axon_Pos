using Axon.Domain.Common;

namespace Axon.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string NameEN { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#FFFFFF";
        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
