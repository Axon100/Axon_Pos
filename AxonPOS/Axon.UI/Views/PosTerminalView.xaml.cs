using System.Windows.Controls;
using System.Windows.Input;
using Axon.UI.ViewModels;

namespace Axon.UI.Views
{
    public partial class PosTerminalView : UserControl
    {
        public PosTerminalView()
        {
            InitializeComponent();
            Loaded += PosTerminalView_Loaded;
            PreviewKeyDown += PosTerminalView_PreviewKeyDown;
        }

        private async void PosTerminalView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is PosTerminalViewModel vm)
            {
                await vm.LoadDataAsync();
            }

            // Automatically set focus on Barcode Scanner input box
            TxtBarcodeInput?.Focus();
        }

        private void PosTerminalView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // If user or scanner sends 'Enter' while not directly in a multiline box, trigger barcode processing
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (DataContext is PosTerminalViewModel vm)
                {
                    if (vm.ProcessBarcodeInputCommand.CanExecute(null))
                    {
                        vm.ProcessBarcodeInputCommand.Execute(null);
                        TxtBarcodeInput?.Focus();
                        TxtBarcodeInput?.SelectAll();
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
