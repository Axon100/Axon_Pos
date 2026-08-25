using Axon.UI.Helpers;
using Axon.UI.ViewModels.Base;
using Axon.Application.Interfaces.Services;
using Axon.Application.Interfaces.Repositories;
using Axon.Domain.Entities;
using Axon.UI.Views;
using Axon.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Axon.UI.ViewModels
{
    public partial class InventoryControlViewModel : BaseViewModel
    {
        [ObservableProperty]
        private int _totalItemsCount;

        [ObservableProperty]
        private int _lowStockAlertsCount;

        [ObservableProperty]
        private int _pendingRestockCount;

        [ObservableProperty]
        private int _warehouseCapacity;

        [ObservableProperty]
        private string _entriesCountDisplay = string.Empty;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isCardViewMode = true; // Default to 3D Box Cards View

        [RelayCommand]
        private void SetCardViewMode() => IsCardViewMode = true;

        [RelayCommand]
        private void SetTableViewMode() => IsCardViewMode = false;

        [ObservableProperty]
        private bool _isLowStockFilterActive;

        private readonly IInventoryService _inventoryService;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private List<InventoryItemModel> _allInventoryItems = new();
        public ObservableCollection<InventoryItemModel> InventoryItems { get; } = new();

        public InventoryControlViewModel(
            IInventoryService inventoryService, 
            IRepository<Product> productRepository,
            IRepository<Category> categoryRepository)
        {
            _inventoryService = inventoryService;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            Title = AppResources.GetString("InventoryControl", "مراقبة المخزون والجرد");

            _ = LoadDataAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        private async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                _allInventoryItems.Clear();
                var products = await _productRepository.GetAllAsync();
                var productList = products.ToList();

                TotalItemsCount = productList.Count;
                LowStockAlertsCount = productList.Count(p => p.CurrentStock > 0 && p.CurrentStock < 10);
                PendingRestockCount = productList.Count(p => p.CurrentStock == 0);
                WarehouseCapacity = TotalItemsCount > 0 ? Math.Min(100, (TotalItemsCount * 100) / 500) : 0;
                EntriesCountDisplay = string.Format(AppResources.GetString("EntriesCountFormat", "{0} عناصر"), TotalItemsCount);

                foreach (var p in productList)
                {
                    string status = AppResources.GetString("Optimal", "مثالي");
                    string icon = "CheckCircle";
                    if (p.CurrentStock == 0)
                    {
                        status = AppResources.GetString("OutOfStock", "نفذت الكمية");
                        icon = "AlertCircle";
                    }
                    else if (p.CurrentStock < 10)
                    {
                        status = AppResources.GetString("LowStock", "مخزون منخفض");
                        icon = "Alert";
                    }

                    _allInventoryItems.Add(new InventoryItemModel
                    {
                        Id = p.Id,
                        Sku = p.SKU,
                        Name = string.IsNullOrEmpty(p.NameAR) ? (p.NameEN ?? $"صنف #{p.Id}") : p.NameAR,
                        ImageUrl = p.ImagePath ?? string.Empty,
                        Location = "المخزن الرئيسي A",
                        CurrentStock = (int)p.CurrentStock,
                        RequiredStock = 50,
                        Status = status,
                        Icon = icon
                    });
                }

                ApplyFilter();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allInventoryItems.AsEnumerable();

            if (IsLowStockFilterActive)
            {
                filtered = filtered.Where(x => x.CurrentStock < 10);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.Trim().ToLower();
                filtered = filtered.Where(x => x.Name.ToLower().Contains(query) || x.Sku.ToLower().Contains(query));
            }

            InventoryItems.Clear();
            foreach (var item in filtered)
            {
                InventoryItems.Add(item);
            }
        }

        [RelayCommand]
        private void ToggleLowStockFilter()
        {
            IsLowStockFilterActive = !IsLowStockFilterActive;
            ApplyFilter();
        }

        [RelayCommand]
        private async Task OpenAddStockAsync()
        {
            if (!UserSessionService.HasPermission("Inventory.StockIn"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لتوريد وتغذية شحنات مخزنية!", "تنبيه الصلاحيات (RBAC)", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var products = (await _productRepository.GetAllAsync()).ToList();
            var categories = (await _categoryRepository.GetAllAsync()).ToList();

            var window = new AddStockWindow();
            window.InitializeData(categories, products);

            if (window.ShowDialog() == true && window.Result != null)
            {
                var res = window.Result;
                Product? matchedProd = null;

                if (res.ProductId.HasValue)
                {
                    matchedProd = await _productRepository.GetByIdAsync(res.ProductId.Value);
                }
                else
                {
                    matchedProd = products.FirstOrDefault(p => (p.NameAR != null && p.NameAR.Equals(res.ItemName, StringComparison.OrdinalIgnoreCase)) ||
                                                               (p.NameEN != null && p.NameEN.Equals(res.ItemName, StringComparison.OrdinalIgnoreCase)));
                }

                if (matchedProd != null)
                {
                    matchedProd.CurrentStock += res.QuantityAdded;
                    if (res.CategoryId.HasValue)
                    {
                        matchedProd.CategoryId = res.CategoryId.Value;
                    }
                    await _productRepository.UpdateAsync(matchedProd);
                }
                else
                {
                    var newProd = new Product
                    {
                        NameAR = res.ItemName,
                        NameEN = res.ItemName,
                        SKU = $"SKU-{new Random().Next(10000, 99999)}",
                        CategoryId = res.CategoryId ?? (categories.FirstOrDefault()?.Id ?? 1),
                        SellingPrice = 0,
                        CostPrice = 0,
                        CurrentStock = res.QuantityAdded,
                        IsActive = true
                    };
                    await _productRepository.AddAsync(newProd);
                }

                await LoadDataAsync();
                AxonMessageBox.Show($"تم توريد وتغذية كمية ({res.QuantityAdded}) للصنف ({res.ItemName}) بنجاح!", "تغذية المخزون", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private async Task CustomRestockAsync(InventoryItemModel item)
        {
            if (item == null) return;
            if (!UserSessionService.HasPermission("Inventory.StockIn"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لتوريد وتغذية كميات للمخزن!", "تنبيه الصلاحيات (RBAC)", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var products = (await _productRepository.GetAllAsync()).ToList();
            var categories = (await _categoryRepository.GetAllAsync()).ToList();

            var window = new AddStockWindow();
            window.InitializeData(categories, products, item.Id);

            if (window.ShowDialog() == true && window.Result != null)
            {
                var res = window.Result;
                var product = await _productRepository.GetByIdAsync(item.Id);
                if (product != null)
                {
                    product.CurrentStock += res.QuantityAdded;
                    if (res.CategoryId.HasValue)
                    {
                        product.CategoryId = res.CategoryId.Value;
                    }
                    await _productRepository.UpdateAsync(product);
                    await LoadDataAsync();
                    AxonMessageBox.Show($"تم تزويد الصنف ({item.Name}) بكمية {res.QuantityAdded} بنجاح!", "تغذية المخزون", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        [RelayCommand]
        private async Task RestockItemAsync(InventoryItemModel item)
        {
            if (item == null) return;
            if (!UserSessionService.HasPermission("Inventory.StockIn"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لإعادة تخزين أصناف المستودع!", "تنبيه الصلاحيات (RBAC)", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var product = await _productRepository.GetByIdAsync(item.Id);
                if (product != null)
                {
                    product.CurrentStock += 50;
                    await _productRepository.UpdateAsync(product);
                    await LoadDataAsync();
                    AxonMessageBox.Show($"تمت زيادة مخزون الصنف ({item.Name}) بمقدار 50 قطعة بنجاح!", "إعادة التخزين", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeductStockAsync(InventoryItemModel item)
        {
            if (item == null) return;
            if (!UserSessionService.HasPermission("Inventory.StockIn"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لخصم كميات من المخزن!", "تنبيه الصلاحيات (RBAC)", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var product = await _productRepository.GetByIdAsync(item.Id);
                if (product != null)
                {
                    if (product.CurrentStock <= 0)
                    {
                        AxonMessageBox.Show($"الصنف ({item.Name}) مخزونه الحالي 0 ولا يمكن الخصم منه!", "تنبيه المخزون", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    product.CurrentStock = Math.Max(0, product.CurrentStock - 1);
                    await _productRepository.UpdateAsync(product);
                    await LoadDataAsync();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteProductAsync(InventoryItemModel item)
        {
            if (item == null) return;
            if (!UserSessionService.HasPermission("Inventory.Delete"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لحذف أصناف الجرد من المخزون!", "تنبيه الصلاحيات (RBAC)", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = AxonMessageBox.Show($"هل أنت متأكد أنك تريد حذف الصنف ({item.Name}) نهائياً من المخزون؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                IsBusy = true;
                try
                {
                    var product = await _productRepository.GetByIdAsync(item.Id);
                    if (product != null)
                    {
                        await _productRepository.DeleteAsync(product);
                        await LoadDataAsync();
                        MessageBox.Show("تم حذف الصنف من المخزون بنجاح!", "حذف صنف", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }

    public class InventoryItemModel
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int RequiredStock { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
