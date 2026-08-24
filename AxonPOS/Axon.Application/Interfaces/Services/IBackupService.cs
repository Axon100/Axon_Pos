using System.Threading.Tasks;

namespace Axon.Application.Interfaces.Services
{
    public interface IBackupService
    {
        Task BackupDatabaseAsync(string destinationPath);
        Task RestoreDatabaseAsync(string backupPath);
    }
}
