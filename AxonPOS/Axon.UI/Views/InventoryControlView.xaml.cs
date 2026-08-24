using System.Windows;
using System.Windows.Controls;
using Axon.UI.ViewModels;

namespace Axon.UI.Views
{
    public partial class InventoryControlView : UserControl
    {
        public InventoryControlView()
        {
            InitializeComponent();
        }

        private void OnThreeDotsMenuClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }
    }
}
