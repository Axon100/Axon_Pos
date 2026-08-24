using System.Threading.Tasks;

namespace Axon.Application.Interfaces.Services
{
    public interface IPrintService
    {
        Task PrintReceiptAsync(int saleId);
        Task PrintReportAsync(string reportName, object data);
    }
}
