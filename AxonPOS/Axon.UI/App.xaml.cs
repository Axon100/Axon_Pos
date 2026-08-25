using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Axon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Axon.Application.Interfaces.Services;
using Axon.Infrastructure.Services;

namespace Axon.UI
{
    public partial class App : System.Windows.Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            InitializeHost();
        }

        private static void InitializeHost()
        {
            var configService = new DatabaseConfigService();
            var connectionString = configService.GetConnectionString();

            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // Add Services
                    services.AddSingleton<IDatabaseConfigService>(configService);

                    // Add DbContext with current dynamic connection string
                    services.AddDbContext<AxonDbContext>(options =>
                    {
                        if (connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
                        {
                            options.UseSqlite(connectionString);
                        }
                        else
                        {
                            options.UseSqlServer(connectionString);
                        }
                    });

                    // Add Views
                    services.AddTransient<MainWindow>();
                    services.AddTransient<Axon.UI.Views.LoginView>();
                    services.AddTransient<Axon.UI.Views.DashboardView>();
                    services.AddTransient<Axon.UI.Views.ProductManagementView>();
                    services.AddTransient<Axon.UI.Views.PosTerminalView>();
                    services.AddTransient<Axon.UI.Views.InventoryControlView>();
                    services.AddTransient<Axon.UI.Views.ExpensesView>();
                    services.AddTransient<Axon.UI.Views.DatabaseSetupWindow>();

                    // Add ViewModels
                    services.AddTransient<Axon.UI.ViewModels.MainWindowViewModel>();
                    services.AddTransient<Axon.UI.ViewModels.LoginViewModel>();
                    services.AddTransient<Axon.UI.ViewModels.DashboardViewModel>();
                    services.AddTransient<Axon.UI.ViewModels.ProductManagementViewModel>();
                    services.AddTransient<Axon.UI.ViewModels.PosTerminalViewModel>();
                    services.AddTransient<Axon.UI.ViewModels.InventoryControlViewModel>();
                    services.AddTransient<Axon.UI.ViewModels.ExpensesViewModel>();
                    services.AddTransient<Axon.UI.ViewModels.ReportsViewModel>();
                    services.AddTransient<Axon.UI.ViewModels.SettingsViewModel>();
                    services.AddTransient<Axon.UI.ViewModels.DatabaseSetupViewModel>();
                    services.AddTransient<Axon.UI.ViewModels.BarcodeManagementViewModel>();
                    
                    // Add Repositories & Business Services
                    services.AddScoped(typeof(Axon.Application.Interfaces.Repositories.IRepository<>), typeof(Axon.Infrastructure.Data.Repositories.Repository<>));
                    services.AddScoped<Axon.Application.Interfaces.Services.IAuthenticationService, Axon.Infrastructure.Services.AuthenticationService>();
                    services.AddScoped<Axon.Application.Interfaces.Services.ISalesService, Axon.Infrastructure.Services.SalesService>();
                    services.AddScoped<Axon.Application.Interfaces.Services.IInventoryService, Axon.Infrastructure.Services.InventoryService>();
                    services.AddScoped<Axon.Application.Interfaces.Services.IPrintService, Axon.UI.Services.PrintService>();
                    services.AddScoped<Axon.Application.Interfaces.Services.IBarcodeService, Axon.Infrastructure.Services.BarcodeService>();
                    services.AddScoped<Axon.Application.Interfaces.Services.IBackupService, Axon.Infrastructure.Services.BackupService>();
                    services.AddScoped<Axon.Application.Interfaces.Services.IPermissionService, Axon.Infrastructure.Services.PermissionService>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Ensure application only exits on explicit user close/shutdown
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Global exception logging & crash prevention
            this.DispatcherUnhandledException += (s, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"[DispatcherUnhandledException] {args.Exception}");
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"[AppDomain UnhandledException] {args.ExceptionObject}");
            };

            // Global UX Enhancement: Auto-Select text & clear zero confusion on focus across ALL TextBoxes in system
            EventManager.RegisterClassHandler(typeof(System.Windows.Controls.TextBox), System.Windows.UIElement.GotFocusEvent, new RoutedEventHandler((s, ev) =>
            {
                if (s is System.Windows.Controls.TextBox tb)
                {
                    tb.SelectAll();
                }
            }));

            EventManager.RegisterClassHandler(typeof(System.Windows.Controls.TextBox), System.Windows.UIElement.PreviewMouseLeftButtonDownEvent, new System.Windows.Input.MouseButtonEventHandler((s, ev) =>
            {
                if (s is System.Windows.Controls.TextBox tb && !tb.IsKeyboardFocusWithin)
                {
                    tb.Focus();
                    ev.Handled = true;
                }
            }));

            await AppHost!.StartAsync();

            var configService = AppHost.Services.GetRequiredService<IDatabaseConfigService>();
            var connStr = configService.GetConnectionString();

            var testResult = await configService.TestConnectionAsync(connStr);

            if (!testResult.Success)
            {
                // Connection failed -> Open Configuration Mode
                var setupVm = AppHost.Services.GetRequiredService<Axon.UI.ViewModels.DatabaseSetupViewModel>();
                var setupWindow = new Axon.UI.Views.DatabaseSetupWindow(setupVm);
                
                var dialogResult = setupWindow.ShowDialog();
                if (dialogResult != true)
                {
                    // User closed window without completing setup
                    Shutdown();
                    return;
                }

                // Re-initialize Host with updated connection string
                await AppHost.StopAsync();
                AppHost.Dispose();
                InitializeHost();
                await AppHost.StartAsync();
            }

            // Ensure Database is Created & Seeded cleanly (Persistent storage)
            try
            {
                using (var scope = AppHost.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AxonDbContext>();
                    await dbContext.Database.EnsureCreatedAsync();

                    // Ensure newly added columns (like IsTaxable) exist on existing databases
                    try
                    {
                        if (dbContext.Database.IsSqlServer())
                        {
                            await dbContext.Database.ExecuteSqlRawAsync(
                                "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'IsTaxable') " +
                                "BEGIN ALTER TABLE Products ADD IsTaxable BIT NOT NULL DEFAULT 0; END; " +
                                "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'TaxAmount') " +
                                "BEGIN ALTER TABLE Products ADD TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0; END;");
                        }
                        else if (dbContext.Database.IsSqlite())
                        {
                            try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN IsTaxable INTEGER NOT NULL DEFAULT 0;"); } catch { }
                            try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN TaxAmount NUMERIC NOT NULL DEFAULT 0;"); } catch { }
                        }
                    }
                    catch { /* column already exists or table not yet created */ }
                }
            }
            catch
            {
                // Fallback to Setup window if database initialization fails
                var setupVm = AppHost.Services.GetRequiredService<Axon.UI.ViewModels.DatabaseSetupViewModel>();
                var setupWindow = new Axon.UI.Views.DatabaseSetupWindow(setupVm);
                setupWindow.ShowDialog();
            }

            var startupWindow = AppHost.Services.GetRequiredService<Axon.UI.Views.LoginView>();
            startupWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (AppHost != null)
            {
                await AppHost.StopAsync();
                AppHost.Dispose();
            }
            base.OnExit(e);
        }
    }
}
