using Axon.Domain.Common;
using System;

namespace Axon.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, GiftCard
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
    }
}
