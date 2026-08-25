using System.Windows.Controls;
using Axon.UI.ViewModels;

namespace Axon.UI.Views
{
    public partial class PosTerminalView : UserControl
    {
        public PosTerminalView()
        {
            InitializeComponent();
            Loaded += PosTerminalView_Loaded;
        }

        private async void PosTerminalView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is PosTerminalViewModel vm)
            {
                await vm.LoadDataAsync();
            }
        }
    }
}
