using System;
using Axon.UI.Views;
using System.Windows;
using Axon.Application.Interfaces.Services;
using Axon.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Axon.UI.ViewModels
{
    public partial class LicenseActivationViewModel : BaseViewModel
    {
        private readonly ILicenseService _licenseService;

        [ObservableProperty]
        private string _hardwareId = string.Empty;

        [ObservableProperty]
        private string _licenseKey = string.Empty;

        [ObservableProperty]
        private string _activationStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isErrorVisible;

        [ObservableProperty]
        private bool _isActivatedSuccessfully;

        public event Action? OnActivated;

        public LicenseActivationViewModel(ILicenseService licenseService)
        {
            _licenseService = licenseService;
            HardwareId = _licenseService.GetHardwareId();
        }

        [RelayCommand]
        private void CopyHardwareId()
        {
            try
            {
                Clipboard.SetText(HardwareId);
                AxonMessageBox.Show("تم نسخ كود الجهاز بنجاح! يمكنك الآن إرساله للمطور.", "تم النسخ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AxonMessageBox.Show($"فشل في النسخ: {ex.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void Activate()
        {
            ActivationStatusMessage = string.Empty;
            IsErrorVisible = false;

            if (_licenseService.ValidateAndActivate(LicenseKey, out var err))
            {
                IsActivatedSuccessfully = true;
                AxonMessageBox.Show("تم تفعيل نظام Axon POS بنجاح! أهلاً بك.", "تفعيل ناجح 🎉", MessageBoxButton.OK, MessageBoxImage.Information);
                OnActivated?.Invoke();
            }
            else
            {
                ActivationStatusMessage = err;
                IsErrorVisible = true;
            }
        }
    }
}
