using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Axon.UI.Views
{
    public partial class DiscountWindow : Window
    {
        public decimal DiscountAmount { get; private set; }
        public bool IsPercentage { get; private set; }

        public DiscountWindow(decimal currentDiscount = 0)
        {
            InitializeComponent();
            TxtDiscountValue.Text = currentDiscount.ToString("0.##");
            TxtDiscountValue.Focus();
            TxtDiscountValue.SelectAll();

            if (System.Windows.Application.Current.MainWindow != null)
            {
                this.Owner = System.Windows.Application.Current.MainWindow;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
                string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
                e.Handled = !regex.IsMatch(newText);
            }
            else
            {
                e.Handled = true;
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(TxtDiscountValue.Text, out decimal val))
            {
                DiscountAmount = val;
                IsPercentage = RadioPercent.IsChecked == true;
                DialogResult = true;
                Close();
            }
            else
            {
                AxonMessageBox.Show("يرجى إدخال رقم خصم صحيح", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
