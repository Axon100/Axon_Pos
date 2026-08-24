using Axon.Domain.Common;

namespace Axon.Domain.Entities
{
    public class StockMovement : BaseEntity
    {
        public int TransactionId { get; set; }
        public InventoryTransaction? Transaction { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public decimal Quantity { get; set; } // Positive for In, Negative for Out
        public decimal UnitCost { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
    }
}
