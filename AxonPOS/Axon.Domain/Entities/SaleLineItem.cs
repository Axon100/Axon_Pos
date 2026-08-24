using Axon.Domain.Common;

namespace Axon.Domain.Entities
{
    public class SaleLineItem : BaseEntity
    {
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
    }
}
