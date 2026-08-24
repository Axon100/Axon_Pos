using System.Threading.Tasks;

namespace Axon.Application.Interfaces.Services
{
    public interface IDatabaseConfigService
    {
        string GetConnectionString();
        void SaveConnectionString(string connectionString);
        string BuildConnectionString(string server, string database, bool useWindowsAuth, string username, string password, string? customConnectionString);
        Task<(bool Success, string Message)> TestConnectionAsync(string connectionString);
        Task<(bool Success, string Message)> CreateAndMigrateDatabaseAsync(string connectionString);
    }
}
