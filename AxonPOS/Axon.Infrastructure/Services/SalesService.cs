using Axon.Application.Interfaces.Repositories;
using Axon.Application.Interfaces.Services;
using Axon.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Axon.Infrastructure.Services
{
    public class SalesService : ISalesService
    {
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<Return> _returnRepository;
        private readonly IRepository<SaleLineItem> _saleLineItemRepository;
        private readonly IInventoryService _inventoryService;

        public SalesService(
            IRepository<Sale> saleRepository, 
            IRepository<Return> returnRepository,
            IRepository<SaleLineItem> saleLineItemRepository,
            IInventoryService inventoryService)
        {
            _saleRepository = saleRepository;
            _returnRepository = returnRepository;
            _saleLineItemRepository = saleLineItemRepository;
            _inventoryService = inventoryService;
        }

        public async Task<decimal> CalculateProfitAsync(Sale sale)
        {
            decimal totalCost = 0m;
            foreach (var item in sale.LineItems)
            {
                var product = await _inventoryService.CheckAvailabilityAsync(item.ProductId, 0);
                totalCost += item.Quantity * item.UnitPrice * 0.7m;
            }
            return Math.Max(0, sale.Total - totalCost);
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            return $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";
        }

        public async Task<Return> ProcessReturnAsync(Return saleReturn)
        {
            var addedReturn = await _returnRepository.AddAsync(saleReturn);

            // Handle restock & increment inventory
            foreach (var item in saleReturn.ReturnLineItems.Where(li => li.RestockToInventory))
            {
                int productId = item.SaleLineItem?.ProductId ?? 0;
                if (productId == 0 && item.SaleLineItemId > 0)
                {
                    var lineItem = await _saleLineItemRepository.GetByIdAsync(item.SaleLineItemId);
                    if (lineItem != null)
                    {
                        productId = lineItem.ProductId;
                    }
                }

                if (productId > 0 && item.QuantityReturned > 0)
                {
                    await _inventoryService.AddStockAsync(
                        productId, 
                        item.QuantityReturned, 
                        $"Return #{addedReturn.Id} - Inv #{saleReturn.SaleId}", 
                        saleReturn.UserId);
                }
            }

            // Update original sale status
            if (saleReturn.SaleId > 0)
            {
                var sale = await _saleRepository.GetByIdAsync(saleReturn.SaleId);
                if (sale != null)
                {
                    sale.Status = "Refunded";
                    await _saleRepository.UpdateAsync(sale);
                }
            }

            return addedReturn;
        }

        public async Task<Sale> ProcessSaleAsync(Sale sale)
        {
            var addedSale = await _saleRepository.AddAsync(sale);

            // Deduct stock with proper cashier auditing
            foreach (var item in sale.LineItems)
            {
                await _inventoryService.DeductStockAsync(item.ProductId, (int)item.Quantity, $"Sale #{addedSale.Id}", sale.CashierId);
            }

            return addedSale;
        }
    }
}
