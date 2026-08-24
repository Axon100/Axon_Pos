using Axon.Domain.Common;

namespace Axon.Domain.Entities
{
    public class UnitOfMeasure : BaseEntity
    {
        public string NameEN { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public bool AllowsDecimals { get; set; }
    }
}
