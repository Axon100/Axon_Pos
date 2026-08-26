using System.Windows;
using Axon.UI.ViewModels;

namespace Axon.UI.Views
{
    public partial class LicenseActivationWindow : Window
    {
        public LicenseActivationWindow(LicenseActivationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.OnActivated += () =>
            {
                DialogResult = true;
                Close();
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
