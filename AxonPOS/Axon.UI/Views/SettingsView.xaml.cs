using System.Windows.Controls;
using Axon.UI.ViewModels;

namespace Axon.UI.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void OldPassword_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
                vm.OldPassword = pb.Password;
        }

        private void NewPassword_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
                vm.NewPassword = pb.Password;
        }

        private void ConfirmPassword_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
                vm.ConfirmPassword = pb.Password;
        }
    }
}
