using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Axon.UI.Views
{
    public partial class DeductStockWindow : Window
    {
        public int QuantityToDeduct { get; private set; } = 0;
        private int _currentStock = 0;

        public DeductStockWindow()
        {
            InitializeComponent();
            
            var main = System.Windows.Application.Current?.MainWindow;
            if (main != null && main != this)
            {
                this.Owner = main;
            }
        }

        public void InitializeData(string productName, int currentStock)
        {
            _currentStock = currentStock;
            TxtProductName.Text = productName;
            TxtCurrentStock.Text = currentStock.ToString();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtDeductQuantity.Text) || !int.TryParse(TxtDeductQuantity.Text, out int qty) || qty <= 0)
            {
                AxonMessageBox.Show("يرجى إدخال كمية خصم صحيحة (أكبر من 0).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (qty > _currentStock)
            {
                AxonMessageBox.Show($"الكمية المراد خصمها ({qty}) أكبر من المتاح حالياً بالرصيد ({_currentStock} قطعة)!", "تنبيه المخزون", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            QuantityToDeduct = qty;
            DialogResult = true;
            Close();
        }
    }
}
