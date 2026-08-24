using Axon.Application.Interfaces.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Axon.Infrastructure.Services
{
    public class PrintService : IPrintService
    {
        public Task PrintReceiptAsync(int saleId)
        {
            Debug.WriteLine($"Printing receipt #{saleId}...");
            return Task.CompletedTask;
        }

        public Task PrintReportAsync(string reportName, object data)
        {
            Debug.WriteLine($"Printing report {reportName}...");
            return Task.CompletedTask;
        }
    }
}
