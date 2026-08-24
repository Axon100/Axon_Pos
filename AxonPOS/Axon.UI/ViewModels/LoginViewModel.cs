using Axon.Application.DTOs.Authentication;
using Axon.Application.Interfaces.Services;
using Axon.UI.Helpers;
using Axon.UI.Services;
using Axon.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Axon.UI.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IPermissionService _permissionService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isPasswordVisible;

        [ObservableProperty]
        private bool _keepMeSignedIn;

        public event System.Action? LoginSucceeded;

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        public LoginViewModel(IAuthenticationService authService, IPermissionService permissionService)
        {
            _authService = authService;
            _permissionService = permissionService;
            Title = AppResources.GetString("Login", "Login");
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = AppResources.GetString("Error", "Please enter both User ID and Password.");
                HasError = true;
                return;
            }

            IsBusy = true;
            HasError = false;

            var request = new LoginRequestDto
            {
                Username = Username,
                Password = Password,
                KeepMeSignedIn = KeepMeSignedIn
            };

            var response = await _authService.LoginAsync(request);

            if (response.Success)
            {
                // Ensure permissions seeded
                await _permissionService.EnsureDefaultPermissionsSeededAsync();
                
                // Fetch effective permissions for logged-in user
                var perms = await _permissionService.GetUserEffectivePermissionsAsync(response.UserId);
                
                UserSessionService.SetSession(response, perms);
                
                IsBusy = false;
                LoginSucceeded?.Invoke();
            }
            else
            {
                IsBusy = false;
                ErrorMessage = response.ErrorMessage;
                HasError = true;
            }
        }
    }
}
