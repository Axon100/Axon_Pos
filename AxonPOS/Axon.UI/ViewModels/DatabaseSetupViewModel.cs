using Axon.Application.Interfaces.Services;
using Axon.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Axon.UI.ViewModels
{
    public partial class DatabaseSetupViewModel : BaseViewModel
    {
        private readonly IDatabaseConfigService _databaseConfigService;

        [ObservableProperty]
        private string _serverName = ".\\SQLEXPRESS";

        [ObservableProperty]
        private string _databaseName = "AxonPOS";

        [ObservableProperty]
        private bool _useWindowsAuth = true;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isAdvancedMode;

        [ObservableProperty]
        private string _fullConnectionString = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isSuccessStatus;

        [ObservableProperty]
        private bool _isStatusVisible;

        public event Action? ConfigurationCompleted;

        public DatabaseSetupViewModel(IDatabaseConfigService databaseConfigService)
        {
            _databaseConfigService = databaseConfigService;
            Title = "إعدادات قاعدة البيانات - وضع التكوين التشغيلي";

            LoadCurrentConfig();
        }

        private void LoadCurrentConfig()
        {
            try
            {
                var connStr = _databaseConfigService.GetConnectionString();
                if (!string.IsNullOrWhiteSpace(connStr))
                {
                    FullConnectionString = connStr;
                }
            }
            catch
            {
                // Fallback
            }
        }

        private string GetActiveConnectionString()
        {
            return _databaseConfigService.BuildConnectionString(
                ServerName,
                DatabaseName,
                UseWindowsAuth,
                Username,
                Password,
                IsAdvancedMode ? FullConnectionString : null
            );
        }

        [RelayCommand]
        private void SetLocalExpressSample()
        {
            IsAdvancedMode = true;
            FullConnectionString = "Data Source=.\\SQLEXPRESS;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=\"AxonPOS\";Command Timeout=0";
        }

        [RelayCommand]
        private void SetCloudDbSample()
        {
            IsAdvancedMode = true;
            FullConnectionString = "Data Source=db42178.public.databaseasp.net;Initial Catalog=db42178;Persist Security Info=False;User ID=db42178;Password=your_password;Pooling=False;MultipleActiveResultSets=True;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;Application Name=AxonPOS;Command Timeout=0";
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            IsBusy = true;
            IsStatusVisible = false;
            try
            {
                var connStr = GetActiveConnectionString();
                var result = await _databaseConfigService.TestConnectionAsync(connStr);
                
                StatusMessage = result.Message;
                IsSuccessStatus = result.Success;
                IsStatusVisible = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            IsBusy = true;
            IsStatusVisible = false;
            try
            {
                var connStr = GetActiveConnectionString();
                _databaseConfigService.SaveConnectionString(connStr);
                
                StatusMessage = "تم حفظ إعدادات الاتصال بنجاح محلياً.";
                IsSuccessStatus = true;
                IsStatusVisible = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"فشل الحفظ: {ex.Message}";
                IsSuccessStatus = false;
                IsStatusVisible = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CreateAndMigrateAsync()
        {
            IsBusy = true;
            IsStatusVisible = false;
            try
            {
                var connStr = GetActiveConnectionString();
                var result = await _databaseConfigService.CreateAndMigrateDatabaseAsync(connStr);

                if (result.Success)
                {
                    _databaseConfigService.SaveConnectionString(connStr);
                }

                StatusMessage = result.Message;
                IsSuccessStatus = result.Success;
                IsStatusVisible = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ContinueAsync()
        {
            var connStr = GetActiveConnectionString();
            var test = await _databaseConfigService.TestConnectionAsync(connStr);
            if (!test.Success)
            {
                StatusMessage = "يرجى اختبار الاتصال والتأكد من نجاحه أو إنشاء قاعدة البيانات أولاً قبل المتابعة.";
                IsSuccessStatus = false;
                IsStatusVisible = true;
                return;
            }

            _databaseConfigService.SaveConnectionString(connStr);
            ConfigurationCompleted?.Invoke();
        }
    }
}
