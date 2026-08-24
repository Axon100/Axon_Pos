using Axon.Domain.Entities;
using System.Threading.Tasks;

namespace Axon.Application.Interfaces.Services
{
    public interface ISalesService
    {
        Task<Sale> ProcessSaleAsync(Sale sale);
        Task<Return> ProcessReturnAsync(Return saleReturn);
        Task<string> GenerateInvoiceNumberAsync();
        Task<decimal> CalculateProfitAsync(Sale sale);
    }
}
