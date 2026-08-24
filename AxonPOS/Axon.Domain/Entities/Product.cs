using Axon.Domain.Common;

namespace Axon.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int UnitId { get; set; }
        public UnitOfMeasure? Unit { get; set; }

        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        
        // Cached value updated via triggers for POS speed
        public decimal CurrentStock { get; set; } 
        public decimal ReorderLevel { get; set; }
        
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsTaxable { get; set; } = true;
    }
}
