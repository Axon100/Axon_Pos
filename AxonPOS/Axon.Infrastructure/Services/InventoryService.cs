using Axon.Application.Interfaces.Repositories;
using Axon.Application.Interfaces.Services;
using Axon.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Axon.Infrastructure.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<InventoryTransaction> _transactionRepository;

        public InventoryService(IRepository<Product> productRepository, IRepository<InventoryTransaction> transactionRepository)
        {
            _productRepository = productRepository;
            _transactionRepository = transactionRepository;
        }

        public async Task AddStockAsync(int productId, int quantity, string referenceNumber, int? userId = null)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product != null)
            {
                product.CurrentStock += quantity;
                await _productRepository.UpdateAsync(product);

                var transaction = new InventoryTransaction
                {
                    ReferenceNumber = referenceNumber,
                    Type = "Adjustment In",
                    Date = DateTime.Now,
                    UserId = (userId.HasValue && userId.Value > 0) ? userId.Value : 1,
                    Movements = new System.Collections.Generic.List<StockMovement>
                    {
                        new StockMovement
                        {
                            ProductId = productId,
                            Quantity = quantity
                        }
                    }
                };
                await _transactionRepository.AddAsync(transaction);
            }
        }

        public async Task<bool> CheckAvailabilityAsync(int productId, int requiredQuantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            return product != null && product.CurrentStock >= requiredQuantity;
        }

        public async Task DeductStockAsync(int productId, int quantity, string referenceNumber, int? userId = null)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product != null)
            {
                product.CurrentStock -= quantity;
                await _productRepository.UpdateAsync(product);

                var transaction = new InventoryTransaction
                {
                    ReferenceNumber = referenceNumber,
                    Type = "Adjustment Out",
                    Date = DateTime.Now,
                    UserId = (userId.HasValue && userId.Value > 0) ? userId.Value : 1,
                    Movements = new System.Collections.Generic.List<StockMovement>
                    {
                        new StockMovement
                        {
                            ProductId = productId,
                            Quantity = -quantity
                        }
                    }
                };
                await _transactionRepository.AddAsync(transaction);
            }
        }
    }
}
