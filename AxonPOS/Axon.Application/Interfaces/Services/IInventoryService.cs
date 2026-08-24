using Axon.Domain.Entities;
using System.Threading.Tasks;

namespace Axon.Application.Interfaces.Services
{
    public interface IInventoryService
    {
        Task DeductStockAsync(int productId, int quantity, string referenceNumber, int? userId = null);
        Task AddStockAsync(int productId, int quantity, string referenceNumber, int? userId = null);
        Task<bool> CheckAvailabilityAsync(int productId, int requiredQuantity);
    }
}
