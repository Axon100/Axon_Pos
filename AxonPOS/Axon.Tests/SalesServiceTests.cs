using Axon.Application.Interfaces.Repositories;
using Axon.Application.Interfaces.Services;
using Axon.Domain.Entities;
using Axon.Infrastructure.Services;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Axon.Tests
{
    public class SalesServiceTests
    {
        [Fact]
        public async Task CalculateProfit_ShouldReturnCorrectAmount()
        {
            // Arrange
            var mockSaleRepo = new Mock<IRepository<Sale>>();
            var mockReturnRepo = new Mock<IRepository<Return>>();
            var mockLineItemRepo = new Mock<IRepository<SaleLineItem>>();
            var mockInventoryService = new Mock<IInventoryService>();

            var service = new SalesService(mockSaleRepo.Object, mockReturnRepo.Object, mockLineItemRepo.Object, mockInventoryService.Object);

            var sale = new Sale
            {
                SubTotal = 100,
                TaxAmount = 10,
                DiscountAmount = 5,
                LineItems = new List<SaleLineItem>
                {
                    new SaleLineItem { ProductId = 1, Quantity = 2, UnitPrice = 50 }
                }
            };

            // Act
            var profit = await service.CalculateProfitAsync(sale);

            // Assert
            Assert.True(profit > 0);
        }

        [Fact]
        public async Task ProcessSale_ShouldDeductInventory()
        {
            // Arrange
            var mockSaleRepo = new Mock<IRepository<Sale>>();
            var mockReturnRepo = new Mock<IRepository<Return>>();
            var mockLineItemRepo = new Mock<IRepository<SaleLineItem>>();
            var mockInventoryService = new Mock<IInventoryService>();

            var service = new SalesService(mockSaleRepo.Object, mockReturnRepo.Object, mockLineItemRepo.Object, mockInventoryService.Object);

            var sale = new Sale
            {
                Id = 1,
                LineItems = new List<SaleLineItem>
                {
                    new SaleLineItem { ProductId = 1, Quantity = 5 }
                }
            };

            mockSaleRepo.Setup(r => r.AddAsync(sale)).ReturnsAsync(sale);

            // Act
            await service.ProcessSaleAsync(sale);

            // Assert
            mockInventoryService.Verify(i => i.DeductStockAsync(1, 5, It.IsAny<string>(), It.IsAny<int?>()), Times.Once);
        }
    }
}
