using System.Windows;
using Axon.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Axon.UI.Views
{
    public partial class LoginView : Window
    {
        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.LoginSucceeded += ViewModel_LoginSucceeded;
        }

        private void ViewModel_LoginSucceeded()
        {
            try
            {
                var mainWindow = App.AppHost!.Services.GetRequiredService<MainWindow>();
                System.Windows.Application.Current.MainWindow = mainWindow;
                if (mainWindow.DataContext is MainWindowViewModel mainVm)
                {
                    mainVm.RefreshPermissions();
                }
                mainWindow.Show();
                this.Close();
            }
            catch (System.Exception ex)
            {
                AxonMessageBox.Show($"خطأ أثناء فتح الواجهة الرئيسية:\n{ex.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool _isUpdatingPassword;

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingPassword) return;
            _isUpdatingPassword = true;
            if (DataContext is LoginViewModel vm && sender is System.Windows.Controls.PasswordBox pb)
            {
                vm.Password = pb.Password;
                if (PlainPasswordBox.Text != pb.Password)
                {
                    PlainPasswordBox.Text = pb.Password;
                }
            }
            _isUpdatingPassword = false;
        }

        private void PlainPasswordBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isUpdatingPassword) return;
            _isUpdatingPassword = true;
            if (DataContext is LoginViewModel vm && sender is System.Windows.Controls.TextBox tb)
            {
                vm.Password = tb.Text;
                if (SecretPasswordBox.Password != tb.Text)
                {
                    SecretPasswordBox.Password = tb.Text;
                }
            }
            _isUpdatingPassword = false;
        }

        private void ToggleEye_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                if (vm.IsPasswordVisible)
                {
                    PlainPasswordBox.Visibility = Visibility.Visible;
                    SecretPasswordBox.Visibility = Visibility.Collapsed;
                    EyeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOff;
                }
                else
                {
                    PlainPasswordBox.Visibility = Visibility.Collapsed;
                    SecretPasswordBox.Visibility = Visibility.Visible;
                    EyeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Eye;
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WhatsApp_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://wa.me/201158906986",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}
