using Axon.UI.ViewModels;
using System.Windows;

namespace Axon.UI.Views
{
    public partial class DatabaseSetupWindow : Window
    {
        public DatabaseSetupWindow(DatabaseSetupViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.ConfigurationCompleted += () =>
            {
                DialogResult = true;
                Close();
            };
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
