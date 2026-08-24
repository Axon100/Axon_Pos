using System.Windows;
using Axon.UI.Services;
using Axon.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Axon.UI.ViewModels
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        [ObservableProperty]
        private BaseViewModel _currentViewModel;

        [ObservableProperty]
        private string _currentUserName = UserSessionService.CurrentUsername;

        [ObservableProperty]
        private string _currentUserRole = UserSessionService.RoleName;

        public bool IsDashboardVisible => UserSessionService.HasPermission("Dashboard.View");
        public bool IsPosVisible => UserSessionService.HasPermission("POS.View");
        public bool IsInventoryVisible => UserSessionService.HasPermission("Inventory.View");
        public bool IsProductsVisible => UserSessionService.HasPermission("Products.View");
        public bool IsBarcodeVisible => UserSessionService.HasPermission("Barcodes.View");
        public bool IsExpensesVisible => UserSessionService.HasPermission("Expenses.View") || UserSessionService.RoleName == "Admin" || UserSessionService.HasPermission("Reports.View");
        public bool IsReportsVisible => UserSessionService.HasPermission("Reports.View");
        public bool IsSettingsVisible => UserSessionService.HasPermission("Settings.View");

        public MainWindowViewModel()
        {
            Title = "Axon-POS";
            // Set default view based on highest permission
            if (IsDashboardVisible)
                _currentViewModel = App.AppHost!.Services.GetRequiredService<DashboardViewModel>();
            else if (IsPosVisible)
                _currentViewModel = App.AppHost!.Services.GetRequiredService<PosTerminalViewModel>();
            else if (IsInventoryVisible)
                _currentViewModel = App.AppHost!.Services.GetRequiredService<InventoryControlViewModel>();
            else
                _currentViewModel = App.AppHost!.Services.GetRequiredService<DashboardViewModel>();
        }

        public void RefreshPermissions()
        {
            CurrentUserName = UserSessionService.CurrentUsername;
            CurrentUserRole = UserSessionService.RoleName;
            OnPropertyChanged(nameof(IsDashboardVisible));
            OnPropertyChanged(nameof(IsPosVisible));
            OnPropertyChanged(nameof(IsInventoryVisible));
            OnPropertyChanged(nameof(IsProductsVisible));
            OnPropertyChanged(nameof(IsBarcodeVisible));
            OnPropertyChanged(nameof(IsExpensesVisible));
            OnPropertyChanged(nameof(IsReportsVisible));
            OnPropertyChanged(nameof(IsSettingsVisible));
        }

        [RelayCommand]
        private void Navigate(string viewName)
        {
            string requiredPermission = viewName switch
            {
                "Dashboard" => "Dashboard.View",
                "PosTerminal" => "POS.View",
                "Inventory" => "Inventory.View",
                "Products" => "Products.View",
                "Barcodes" => "Barcodes.View",
                "Expenses" => "Expenses.View",
                "Reports" => "Reports.View",
                "Settings" => "Settings.View",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(requiredPermission) && !UserSessionService.HasPermission(requiredPermission) && UserSessionService.RoleName != "Admin")
            {
                MessageBox.Show("ليس لديك صلاحية للوصول إلى هذه الصفحة!", "تنبيه الصلاحيات (RBAC)", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            switch (viewName)
            {
                case "Dashboard":
                    CurrentViewModel = App.AppHost!.Services.GetRequiredService<DashboardViewModel>();
                    break;
                case "PosTerminal":
                    CurrentViewModel = App.AppHost!.Services.GetRequiredService<PosTerminalViewModel>();
                    break;
                case "Inventory":
                    CurrentViewModel = App.AppHost!.Services.GetRequiredService<InventoryControlViewModel>();
                    break;
                case "Products":
                    CurrentViewModel = App.AppHost!.Services.GetRequiredService<ProductManagementViewModel>();
                    break;
                case "Barcodes":
                    CurrentViewModel = App.AppHost!.Services.GetRequiredService<BarcodeManagementViewModel>();
                    break;
                case "Expenses":
                    CurrentViewModel = App.AppHost!.Services.GetRequiredService<ExpensesViewModel>();
                    break;
                case "Reports":
                    CurrentViewModel = App.AppHost!.Services.GetRequiredService<ReportsViewModel>();
                    break;
                case "Settings":
                    CurrentViewModel = App.AppHost!.Services.GetRequiredService<SettingsViewModel>();
                    break;
            }
        }
        
        [RelayCommand]
        private void Logout()
        {
            UserSessionService.ClearSession();
            var loginView = App.AppHost!.Services.GetRequiredService<Views.LoginView>();
            loginView.Show();
            System.Windows.Application.Current.Windows[0].Close();
        }
    }
}
