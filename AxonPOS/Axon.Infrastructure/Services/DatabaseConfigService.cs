using Axon.Application.Interfaces.Services;
using Axon.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Axon.Infrastructure.Services
{
    public class DatabaseConfigService : IDatabaseConfigService
    {
        private const string DefaultSqlServerConnectionString = "Data Source=.\\SQLEXPRESS;Initial Catalog=AxonPOS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        private string DefaultSqliteConnectionString
        {
            get
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appData, "AxonPOS");
                Directory.CreateDirectory(folder);
                var dbFile = Path.Combine(folder, "AxonPOS.db");
                return $"Data Source={dbFile}";
            }
        }

        private string ConfigFilePath
        {
            get
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appData, "AxonPOS");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "dbconfig.json");
            }
        }

        public string GetConnectionString()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("ConnectionString", out var connProp))
                    {
                        var value = connProp.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }

            // Test if SQL Server is available locally
            try
            {
                using var conn = new SqlConnection(DefaultSqlServerConnectionString);
                conn.Open();
                return DefaultSqlServerConnectionString;
            }
            catch
            {
                // Local SQL Server not running -> Fallback to permanent local SQLite database
                return DefaultSqliteConnectionString;
            }
        }

        public void SaveConnectionString(string connectionString)
        {
            try
            {
                var data = new { ConnectionString = connectionString };
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"فشل حفظ ملف الاتصال المحلي: {ex.Message}", ex);
            }
        }

        public string BuildConnectionString(string server, string database, bool useWindowsAuth, string username, string password, string? customConnectionString)
        {
            if (!string.IsNullOrWhiteSpace(customConnectionString))
            {
                return customConnectionString.Trim();
            }

            var cleanServer = server?.Trim() ?? string.Empty;
            if (cleanServer.EndsWith("\\") || cleanServer.EndsWith("/"))
            {
                cleanServer = cleanServer.TrimEnd('\\', '/');
            }
            if (cleanServer.Equals("SQLEXPRESS", StringComparison.OrdinalIgnoreCase))
            {
                cleanServer = ".\\SQLEXPRESS";
            }

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = string.IsNullOrWhiteSpace(cleanServer) ? ".\\SQLEXPRESS" : cleanServer,
                InitialCatalog = string.IsNullOrWhiteSpace(database) ? "AxonPOS" : database.Trim(),
                IntegratedSecurity = useWindowsAuth,
                Encrypt = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };

            if (!useWindowsAuth)
            {
                builder.UserID = username?.Trim() ?? string.Empty;
                builder.Password = password ?? string.Empty;
            }

            return builder.ConnectionString;
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return (false, "سلسلة الاتصال فارغة.");
            }

            if (connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var sqliteConn = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
                    await sqliteConn.OpenAsync();
                    return (true, "تم الاتصال بقاعدة البيانات المحلية (SQLite) بنجاح.");
                }
                catch (Exception ex)
                {
                    return (false, $"فشل الاتصال بقاعدة البيانات المحلية:\n{ex.Message}");
                }
            }

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                return (true, "تم الاتصال بخادم SQL Server بنجاح.");
            }
            catch (Exception ex)
            {
                return (false, $"فشل الاتصال بخادم SQL Server:\n{ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CreateAndMigrateDatabaseAsync(string connectionString)
        {
            var test = await TestConnectionAsync(connectionString);
            if (!test.Success && !connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
            {
                // Try testing master connection to verify SQL Server instance exists
                try
                {
                    var builder = new SqlConnectionStringBuilder(connectionString)
                    {
                        InitialCatalog = "master"
                    };
                    using var masterConn = new SqlConnection(builder.ConnectionString);
                    await masterConn.OpenAsync();
                }
                catch (Exception masterEx)
                {
                    return (false, $"تعذر الوصول إلى خادم SQL Server:\n{masterEx.Message}");
                }
            }

            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<AxonDbContext>();
                if (connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
                {
                    optionsBuilder.UseSqlite(connectionString);
                }
                else
                {
                    optionsBuilder.UseSqlServer(connectionString);
                }

                using var dbContext = new AxonDbContext(optionsBuilder.Options);
                await dbContext.Database.EnsureCreatedAsync();

                return (true, "تم إنشاء قاعدة البيانات وتنفيذ الترحيلات وبذر البيانات الأساسية بنجاح.");
            }
            catch (Exception ex)
            {
                return (false, $"فشل إنشاء/ترحيل قاعدة البيانات:\n{ex.Message}");
            }
        }
    }
}
