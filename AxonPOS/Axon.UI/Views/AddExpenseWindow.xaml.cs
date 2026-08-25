using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Axon.UI.ViewModels;

namespace Axon.UI.Views
{
    public partial class AddExpenseWindow : Window
    {
        public ExpenseItemViewModel? Result { get; private set; }

        public AddExpenseWindow()
        {
            InitializeComponent();
            
            // Set Owner to MainWindow safely
            var main = System.Windows.Application.Current?.MainWindow;
            if (main != null && main != this)
            {
                this.Owner = main;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtAmount.Text) || !decimal.TryParse(TxtAmount.Text, out decimal amount))
            {
                AxonMessageBox.Show("يرجى إدخال مبلغ صحيح.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string category = string.IsNullOrWhiteSpace(TxtCategory.Text) ? "مصاريف تشغيلية" : TxtCategory.Text;
            string payment = string.IsNullOrWhiteSpace(TxtPayment.Text) ? "النقدية" : TxtPayment.Text;
            string desc = TxtDescription.Text;

            Result = new ExpenseItemViewModel
            {
                DocNumber = $"EXP-202608{new Random().Next(10, 99)}-{new Random().Next(1000, 9999)}",
                Category = category,
                PaymentMethod = payment,
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Amount = amount,
                Description = string.IsNullOrWhiteSpace(desc) ? $"سداد قيمة مصروف: {category}" : desc
            };

            DialogResult = true;
            Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
