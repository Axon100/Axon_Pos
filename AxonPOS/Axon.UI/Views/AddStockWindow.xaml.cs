using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Axon.Domain.Entities;
using Axon.UI.ViewModels;

namespace Axon.UI.Views
{
    public class CategoryOptionModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProductOptionModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public int CurrentStock { get; set; }

        public override string ToString() => Name;
    }

    public partial class AddStockWindow : Window
    {
        public InventoryStockLogViewModel? Result { get; private set; }

        private List<CategoryOptionModel> _categories = new();
        private List<ProductOptionModel> _allProducts = new();
        private bool _isUpdatingSelection = false;

        public AddStockWindow()
        {
            InitializeComponent();
            if (System.Windows.Application.Current.MainWindow != null)
            {
                this.Owner = System.Windows.Application.Current.MainWindow;
            }
        }

        public void InitializeData(IEnumerable<Category> categories, IEnumerable<Product> products, int? selectedProductId = null)
        {
            _categories = categories.Select(c => new CategoryOptionModel
            {
                Id = c.Id,
                Name = string.IsNullOrEmpty(c.NameAR) ? (c.NameEN ?? $"قسم #{c.Id}") : c.NameAR
            }).ToList();

            _allProducts = products.Select(p => new ProductOptionModel
            {
                Id = p.Id,
                Name = string.IsNullOrEmpty(p.NameAR) ? (p.NameEN ?? $"منتج #{p.Id}") : p.NameAR,
                Sku = p.SKU,
                CategoryId = p.CategoryId,
                CurrentStock = (int)p.CurrentStock
            }).ToList();

            CmbCategory.ItemsSource = _categories;
            CmbProducts.ItemsSource = _allProducts;

            if (selectedProductId.HasValue)
            {
                var target = _allProducts.FirstOrDefault(p => p.Id == selectedProductId.Value);
                if (target != null)
                {
                    CmbProducts.SelectedItem = target;
                    if (target.CategoryId.HasValue)
                    {
                        CmbCategory.SelectedValue = target.CategoryId.Value;
                    }
                }
            }
            else if (_categories.Count > 0)
            {
                CmbCategory.SelectedIndex = 0;
            }
        }

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;
            if (CmbCategory.SelectedValue is int categoryId)
            {
                _isUpdatingSelection = true;
                var filtered = _allProducts.Where(p => p.CategoryId == categoryId).ToList();
                CmbProducts.ItemsSource = filtered.Count > 0 ? filtered : _allProducts;
                _isUpdatingSelection = false;
            }
        }

        private void CmbProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;
            if (CmbProducts.SelectedItem is ProductOptionModel selectedProd)
            {
                _isUpdatingSelection = true;
                if (selectedProd.CategoryId.HasValue)
                {
                    CmbCategory.SelectedValue = selectedProd.CategoryId.Value;
                }
                TxtCurrentStock.Text = $"المتوفر حالياً: {selectedProd.CurrentStock} قطعة";
                _isUpdatingSelection = false;
            }
            else
            {
                TxtCurrentStock.Text = string.Empty;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string itemName = string.Empty;
            int? productId = null;
            int? categoryId = CmbCategory.SelectedValue as int?;
            string categoryName = (CmbCategory.SelectedItem as CategoryOptionModel)?.Name ?? string.Empty;

            if (CmbProducts.SelectedItem is ProductOptionModel selectedProd)
            {
                itemName = selectedProd.Name;
                productId = selectedProd.Id;
                if (!categoryId.HasValue) categoryId = selectedProd.CategoryId;
            }
            else if (!string.IsNullOrWhiteSpace(CmbProducts.Text))
            {
                itemName = CmbProducts.Text.Trim();
                var matched = _allProducts.FirstOrDefault(p => p.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    productId = matched.Id;
                    if (!categoryId.HasValue) categoryId = matched.CategoryId;
                }
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                MessageBox.Show("يرجى اختيار أو كتابة اسم الصنف المراد توريده.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtQuantity.Text) || !int.TryParse(TxtQuantity.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("يرجى إدخال كمية مضافة صحيحة (أكبر من 0).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Result = new InventoryStockLogViewModel
            {
                ProductId = productId,
                ItemName = itemName,
                CategoryId = categoryId,
                CategoryName = categoryName,
                DocNumber = $"STK-ADD-{new Random().Next(1000, 9999)}",
                QuantityAdded = qty,
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            DialogResult = true;
            Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
