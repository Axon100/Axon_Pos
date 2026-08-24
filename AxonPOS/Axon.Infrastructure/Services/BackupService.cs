using Axon.Application.Interfaces.Services;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Axon.Infrastructure.Services
{
    public class BackupService : IBackupService
    {
        private readonly IDatabaseConfigService _databaseConfigService;

        public BackupService(IDatabaseConfigService databaseConfigService)
        {
            _databaseConfigService = databaseConfigService;
        }

        public async Task BackupDatabaseAsync(string destinationPath)
        {
            var connStr = _databaseConfigService.GetConnectionString();

            if (connStr.Contains(".db", StringComparison.OrdinalIgnoreCase))
            {
                // SQLite Backup -> File Copy
                var dbFilePath = ExtractSqliteFilePath(connStr);
                if (File.Exists(dbFilePath))
                {
                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    File.Copy(dbFilePath, destinationPath, overwrite: true);
                    await Task.CompletedTask;
                    return;
                }
            }

            // SQL Server Backup
            var dbName = "AxonPOS";
            var backupQuery = $"BACKUP DATABASE [{dbName}] TO DISK = '{destinationPath}' WITH FORMAT, MEDIANAME = 'AxonPOSBackup', NAME = 'Full Backup of AxonPOS';";

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand(backupQuery, connection);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task RestoreDatabaseAsync(string backupPath)
        {
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود.", backupPath);
            }

            var connStr = _databaseConfigService.GetConnectionString();

            if (connStr.Contains(".db", StringComparison.OrdinalIgnoreCase))
            {
                // SQLite Restore -> Replace .db File
                var dbFilePath = ExtractSqliteFilePath(connStr);
                var dir = Path.GetDirectoryName(dbFilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.Copy(backupPath, dbFilePath, overwrite: true);
                await Task.CompletedTask;
                return;
            }

            // SQL Server Restore
            var dbName = "AxonPOS";
            var restoreQuery = $@"
                USE master;
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                RESTORE DATABASE [{dbName}] FROM DISK = '{backupPath}' WITH REPLACE;
                ALTER DATABASE [{dbName}] SET MULTI_USER;";

            var builder = new SqlConnectionStringBuilder(connStr) { InitialCatalog = "master" };
            using var connection = new SqlConnection(builder.ConnectionString);
            using var command = new SqlCommand(restoreQuery, connection);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        private string ExtractSqliteFilePath(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring("Data Source=".Length).Trim();
                }
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "AxonPOS", "AxonPOS.db");
        }
    }
}
