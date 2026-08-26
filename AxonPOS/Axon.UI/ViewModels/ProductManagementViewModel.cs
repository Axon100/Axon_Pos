using Axon.UI.Helpers;
using Axon.UI.ViewModels.Base;
using Axon.Application.Interfaces.Services;
using Axon.Application.Interfaces.Repositories;
using Axon.Domain.Entities;
using Axon.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Axon.UI.Views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Axon.UI.ViewModels
{
    public partial class ProductManagementViewModel : BaseViewModel
    {
        // 0 = Products, 1 = Categories
        [ObservableProperty]
        private int _selectedTab = 0;

        [ObservableProperty]
        private bool _isCardViewMode = true; // Default to 3D Cards Box View

        [RelayCommand]
        private void SetCardViewMode() => IsCardViewMode = true;

        [RelayCommand]
        private void SetTableViewMode() => IsCardViewMode = false;

        [ObservableProperty]
        private bool _isCategoryCardViewMode = true; // Default to 3D Cards Box View for Categories

        [RelayCommand]
        private void SetCategoryCardViewMode() => IsCategoryCardViewMode = true;

        [RelayCommand]
        private void SetCategoryTableViewMode() => IsCategoryCardViewMode = false;

        [ObservableProperty]
        private string _showingProductsCountDisplay = string.Empty;

        [ObservableProperty]
        private string _paginationCountDisplay = string.Empty;

        // Add / Edit Product Form Properties
        [ObservableProperty]
        private bool _isAddProductDialogOpen;

        [ObservableProperty]
        private string _newProductNameAR = string.Empty;

        [ObservableProperty]
        private string _newProductNameEN = string.Empty;

        [ObservableProperty]
        private string _newProductSKU = string.Empty;

        [ObservableProperty]
        private string _newProductBarcode = string.Empty;

        [ObservableProperty]
        private decimal _newProductSellingPrice;

        [ObservableProperty]
        private decimal _newProductCostPrice;

        [ObservableProperty]
        private decimal _newProductStock;

        [ObservableProperty]
        private string _newProductImagePath = string.Empty;

        [ObservableProperty]
        private int _newProductCategoryId;

        [ObservableProperty]
        private bool _newProductIsTaxable = false; // Default to 'بدون ضريبة'

        [ObservableProperty]
        private decimal _newProductTaxAmount = 0; // Value in EGP

        public bool NewProductIsNotTaxable
        {
            get => !NewProductIsTaxable;
            set
            {
                if (NewProductIsTaxable == value)
                {
                    NewProductIsTaxable = !value;
                    OnPropertyChanged(nameof(NewProductIsNotTaxable));
                }
            }
        }

        partial void OnNewProductIsTaxableChanged(bool value)
        {
            OnPropertyChanged(nameof(NewProductIsNotTaxable));
        }

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private int? _editingProductId;

        [ObservableProperty]
        private string _dialogTitle = "إضافة منتج جديد";

        // Category Management Properties
        [ObservableProperty]
        private bool _isAddCategoryDialogOpen;

        [ObservableProperty]
        private bool _isEditCategoryMode;

        [ObservableProperty]
        private int? _editingCategoryId;

        [ObservableProperty]
        private string _categoryDialogTitle = "إضافة قسم جديد";

        [ObservableProperty]
        private string _newCategoryNameAR = string.Empty;

        [ObservableProperty]
        private string _newCategoryNameEN = string.Empty;

        [ObservableProperty]
        private string _categorySearchText = string.Empty;

        [ObservableProperty]
        private string _productSearchText = string.Empty;

        [ObservableProperty]
        private int _totalCategoriesCount;

        [ObservableProperty]
        private int _activeCategoriesCount;

        partial void OnCategorySearchTextChanged(string value)
        {
            FilterCategories();
        }

        partial void OnProductSearchTextChanged(string value)
        {
            ApplyProductFilters();
        }

        private readonly IInventoryService _inventoryService;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<UnitOfMeasure> _unitRepository;

        public ObservableCollection<ProductManagementItemModel> Products { get; } = new();
        private List<ProductManagementItemModel> _allProductItems = new();
        public ObservableCollection<CategoryFilterModel> Categories { get; } = new();
        public ObservableCollection<CategoryDropdownItem> DialogCategories { get; } = new();

        public ObservableCollection<CategoryItemModel> CategoryItems { get; } = new();
        private List<CategoryItemModel> _allCategoryItems = new();

        public ProductManagementViewModel(
            IInventoryService inventoryService, 
            IRepository<Product> productRepository,
            IRepository<Category> categoryRepository,
            IRepository<UnitOfMeasure> unitRepository)
        {
            _inventoryService = inventoryService;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _unitRepository = unitRepository;
            Title = AppResources.GetString("Products", "إدارة المنتجات");

            _ = LoadDataAsync();
        }

        // ==================== PRODUCT ACTIONS ====================

        [RelayCommand]
        private void OpenAddProductDialog()
        {
            EditingProductId = null;
            IsEditMode = false;
            DialogTitle = "إضافة منتج جديد";
            NewProductNameAR = string.Empty;
            NewProductNameEN = string.Empty;
            NewProductSKU = string.Empty;
            NewProductBarcode = string.Empty;
            NewProductSellingPrice = 0;
            NewProductCostPrice = 0;
            NewProductStock = 0;
            NewProductImagePath = string.Empty;
            NewProductCategoryId = DialogCategories.Count > 0 ? DialogCategories[0].Id : 0;
            NewProductIsTaxable = false;
            NewProductTaxAmount = 0;
            IsAddProductDialogOpen = true;
        }

        [RelayCommand]
        private async Task OpenEditProductDialogAsync(ProductManagementItemModel item)
        {
            if (item == null) return;
            var product = await _productRepository.GetByIdAsync(item.Id);
            if (product == null) return;

            EditingProductId = product.Id;
            IsEditMode = true;
            DialogTitle = "تعديل بيانات المنتج";
            NewProductNameAR = product.NameAR;
            NewProductNameEN = product.NameEN;
            NewProductSKU = product.SKU;
            NewProductBarcode = product.Barcode;
            NewProductSellingPrice = product.SellingPrice;
            NewProductCostPrice = product.CostPrice;
            NewProductStock = product.CurrentStock;
            NewProductImagePath = product.ImagePath ?? string.Empty;
            NewProductCategoryId = product.CategoryId;
            NewProductIsTaxable = product.IsTaxable;
            NewProductTaxAmount = product.TaxAmount;
            IsAddProductDialogOpen = true;
        }

        [RelayCommand]
        private void BrowseProductImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ملفات الصور (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|كافة الملفات (*.*)|*.*",
                Title = "اختر صورة للمنتج"
            };

            if (dialog.ShowDialog() == true)
            {
                NewProductImagePath = dialog.FileName;
            }
        }

        [RelayCommand]
        private void CloseAddProductDialog()
        {
            IsAddProductDialogOpen = false;
        }

        [RelayCommand]
        private async Task SaveProductAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProductNameAR))
            {
                AxonMessageBox.Show("يرجى إدخال اسم المنتج بالعربي!", "حقل مطلوب", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (IsEditMode && !UserSessionService.HasPermission("Products.Edit"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لتعديل بيانات المنتجات!", "تنبيه الصلاحيات (RBAC)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!IsEditMode && !UserSessionService.HasPermission("Products.Add"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لإضافة منتجات جديدة!", "تنبيه الصلاحيات (RBAC)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var categories = await _categoryRepository.GetAllAsync();
                int? categoryId = (NewProductCategoryId > 0) ? NewProductCategoryId : categories.FirstOrDefault()?.Id;
                if (!categoryId.HasValue)
                {
                    var newCat = await _categoryRepository.AddAsync(new Category { NameAR = "عام", NameEN = "General" });
                    categoryId = newCat.Id;
                }

                var units = await _unitRepository.GetAllAsync();
                var unitId = units.FirstOrDefault()?.Id;
                if (!unitId.HasValue)
                {
                    var newUnit = await _unitRepository.AddAsync(new UnitOfMeasure { NameAR = "قطعة", NameEN = "Piece", Abbreviation = "Pcs" });
                    unitId = newUnit.Id;
                }

                if (IsEditMode && EditingProductId.HasValue)
                {
                    var existingProduct = await _productRepository.GetByIdAsync(EditingProductId.Value);
                    if (existingProduct != null)
                    {
                        existingProduct.NameAR = NewProductNameAR;
                        existingProduct.NameEN = string.IsNullOrWhiteSpace(NewProductNameEN) ? NewProductNameAR : NewProductNameEN;
                        existingProduct.SKU = string.IsNullOrWhiteSpace(NewProductSKU) ? existingProduct.SKU : NewProductSKU;
                        existingProduct.Barcode = NewProductBarcode;
                        existingProduct.SellingPrice = NewProductSellingPrice;
                        existingProduct.CostPrice = NewProductCostPrice;
                        existingProduct.CurrentStock = NewProductStock;
                        existingProduct.ImagePath = NewProductImagePath;
                        existingProduct.CategoryId = categoryId.Value;
                        existingProduct.IsTaxable = NewProductIsTaxable;
                        existingProduct.TaxAmount = NewProductIsTaxable ? NewProductTaxAmount : 0;

                        await _productRepository.UpdateAsync(existingProduct);
                    }
                }
                else
                {
                    var product = new Product
                    {
                        NameAR = NewProductNameAR,
                        NameEN = string.IsNullOrWhiteSpace(NewProductNameEN) ? NewProductNameAR : NewProductNameEN,
                        SKU = string.IsNullOrWhiteSpace(NewProductSKU) ? $"SKU-{DateTime.Now.Ticks % 100000}" : NewProductSKU,
                        Barcode = NewProductBarcode,
                        SellingPrice = NewProductSellingPrice,
                        CostPrice = NewProductCostPrice,
                        CurrentStock = NewProductStock,
                        ImagePath = NewProductImagePath,
                        CategoryId = categoryId.Value,
                        UnitId = unitId.Value,
                        IsActive = true,
                        IsTaxable = NewProductIsTaxable,
                        TaxAmount = NewProductIsTaxable ? NewProductTaxAmount : 0
                    };

                    await _productRepository.AddAsync(product);
                }

                IsAddProductDialogOpen = false;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                AxonMessageBox.Show(
                    $"فشل حفظ المنتج: {ex.Message}",
                    "خطأ في الحفظ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteProductAsync(ProductManagementItemModel item)
        {
            if (item == null) return;

            if (!UserSessionService.HasPermission("Products.Delete"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لحذف المنتجات من النظام!", "تنبيه الصلاحيات (RBAC)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var confirm = AxonMessageBox.Show($"هل أنت متأكد أنك تريد حذف المنتج ({item.Name}) نهائياً؟", "تأكيد الحذف", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var entity = await _productRepository.GetByIdAsync(item.Id);
                if (entity != null)
                {
                    await _productRepository.DeleteAsync(entity);
                    await LoadDataAsync();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== CATEGORY ACTIONS ====================

        [RelayCommand]
        private void OpenAddCategoryDialog()
        {
            EditingCategoryId = null;
            IsEditCategoryMode = false;
            CategoryDialogTitle = "إضافة قسم جديد";
            NewCategoryNameAR = string.Empty;
            NewCategoryNameEN = string.Empty;
            IsAddCategoryDialogOpen = true;
        }

        [RelayCommand]
        private void OpenEditCategoryDialog(CategoryItemModel item)
        {
            if (item == null) return;
            EditingCategoryId = item.Id;
            IsEditCategoryMode = true;
            CategoryDialogTitle = "تعديل بيانات القسم";
            NewCategoryNameAR = item.Name;
            NewCategoryNameEN = item.Description;
            IsAddCategoryDialogOpen = true;
        }

        [RelayCommand]
        private void CloseCategoryDialog()
        {
            IsAddCategoryDialogOpen = false;
        }

        [RelayCommand]
        private async Task SaveCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryNameAR))
            {
                AxonMessageBox.Show("يرجى إدخال اسم القسم بالعربي!", "حقل مطلوب", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!UserSessionService.HasPermission("Products.Add") && !UserSessionService.HasPermission("Products.Edit"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لإدارة الأقسام!", "تنبيه الصلاحيات (RBAC)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditCategoryMode && EditingCategoryId.HasValue)
                {
                    var existingCat = await _categoryRepository.GetByIdAsync(EditingCategoryId.Value);
                    if (existingCat != null)
                    {
                        existingCat.NameAR = NewCategoryNameAR;
                        existingCat.NameEN = string.IsNullOrWhiteSpace(NewCategoryNameEN) ? NewCategoryNameAR : NewCategoryNameEN;
                        await _categoryRepository.UpdateAsync(existingCat);
                    }
                }
                else
                {
                    var newCat = new Category
                    {
                        NameAR = NewCategoryNameAR,
                        NameEN = string.IsNullOrWhiteSpace(NewCategoryNameEN) ? NewCategoryNameAR : NewCategoryNameEN
                    };
                    await _categoryRepository.AddAsync(newCat);
                }

                IsAddCategoryDialogOpen = false;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                AxonMessageBox.Show($"فشل حفظ القسم: {ex.Message}", "خطأ في الحفظ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteCategoryAsync(CategoryItemModel item)
        {
            if (item == null) return;

            if (!UserSessionService.HasPermission("Products.Delete"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لحذف الأقسام!", "تنبيه الصلاحيات (RBAC)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (item.ProductsCount > 0)
            {
                var warnResult = AxonMessageBox.Show(
                    $"هذا القسم يحتوي على ({item.ProductsCount}) منتج مرتبط به! هل تريد حذفه وتعيين منتجاته كقسم عام؟",
                    "تحذير وجود منتجات مرتبطة",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (warnResult != System.Windows.MessageBoxResult.Yes) return;
            }
            else
            {
                var confirm = AxonMessageBox.Show(
                    $"هل أنت متأكد أنك تريد حذف القسم ({item.Name}) نهائياً؟",
                    "تأكيد الحذف",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (confirm != System.Windows.MessageBoxResult.Yes) return;
            }

            IsBusy = true;
            try
            {
                var cat = await _categoryRepository.GetByIdAsync(item.Id);
                if (cat != null)
                {
                    await _categoryRepository.DeleteAsync(cat);
                    await LoadDataAsync();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== LOAD & FILTER ====================

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                Categories.Clear();
                DialogCategories.Clear();
                _allCategoryItems.Clear();
                CategoryItems.Clear();
                _allProductItems.Clear();
                Products.Clear();

                var categoryEntities = await _categoryRepository.GetAllAsync();
                var categoryMap = new Dictionary<int, string>();
                
                var products = await _productRepository.GetAllAsync();
                var productList = products.ToList();

                foreach (var c in categoryEntities)
                {
                    var displayName = string.IsNullOrEmpty(c.NameAR) ? c.NameEN : c.NameAR;
                    categoryMap[c.Id] = displayName;
                    Categories.Add(new CategoryFilterModel 
                    { 
                        Id = c.Id, 
                        Name = displayName, 
                        IsChecked = true,
                        OnFilterChanged = ApplyProductFilters 
                    });
                    DialogCategories.Add(new CategoryDropdownItem { Id = c.Id, Name = displayName });

                    var pCount = productList.Count(p => p.CategoryId == c.Id);
                    var catItem = new CategoryItemModel
                    {
                        Id = c.Id,
                        Name = displayName,
                        Description = string.IsNullOrEmpty(c.NameEN) ? displayName : c.NameEN,
                        ProductsCount = pCount,
                        IsActive = true
                    };
                    _allCategoryItems.Add(catItem);
                }

                FilterCategories();
                TotalCategoriesCount = _allCategoryItems.Count;
                ActiveCategoriesCount = _allCategoryItems.Count(c => c.IsActive);

                foreach (var p in productList)
                {
                    string status = AppResources.GetString("InStock", "متوفر");
                    if (p.CurrentStock == 0)
                        status = AppResources.GetString("OutOfStock", "نفذت الكمية");
                    else if (p.CurrentStock < 10)
                        status = AppResources.GetString("LowStock", "مخزون منخفض");

                    var catName = categoryMap.TryGetValue(p.CategoryId, out var cn) ? cn : "عام";

                    _allProductItems.Add(new ProductManagementItemModel
                    {
                        Id = p.Id,
                        Name = string.IsNullOrEmpty(p.NameAR) ? p.NameEN : p.NameAR,
                        Sku = p.SKU,
                        Category = catName,
                        Price = p.SellingPrice,
                        Stock = (int)p.CurrentStock,
                        Status = status,
                        ImageUrl = string.IsNullOrEmpty(p.ImagePath) ? null : p.ImagePath
                    });
                }

                ApplyProductFilters();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyProductFilters()
        {
            var checkedCategories = Categories.Where(c => c.IsChecked).Select(c => c.Name).ToHashSet();
            var filtered = _allProductItems.AsEnumerable();

            // Category filter: if some are checked, filter by them. If none checked, show empty.
            if (Categories.Count > 0)
            {
                if (checkedCategories.Count == 0)
                {
                    filtered = Enumerable.Empty<ProductManagementItemModel>();
                }
                else if (checkedCategories.Count < Categories.Count)
                {
                    filtered = filtered.Where(p => checkedCategories.Contains(p.Category));
                }
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(ProductSearchText))
            {
                var q = ProductSearchText.Trim();
                filtered = filtered.Where(p =>
                    p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    p.Sku.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            var list = filtered.ToList();
            Products.Clear();
            foreach (var item in list)
            {
                Products.Add(item);
            }

            ShowingProductsCountDisplay = string.Format(AppResources.GetString("ShowingProductsFormat", "عرض {0} منتج"), list.Count);
            PaginationCountDisplay = list.Count > 0 ? $"1-{list.Count} من أصل {list.Count}" : "0 من أصل 0";
        }

        private void FilterCategories()
        {
            var filtered = _allCategoryItems.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(CategorySearchText))
            {
                var q = CategorySearchText.Trim();
                filtered = filtered.Where(c => 
                    c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || 
                    c.Description.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            CategoryItems.Clear();
            foreach (var item in filtered)
            {
                CategoryItems.Add(item);
            }
        }
    }

    public partial class CategoryFilterModel : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isChecked = true;

        public Action? OnFilterChanged { get; set; }

        partial void OnIsCheckedChanged(bool value)
        {
            OnFilterChanged?.Invoke();
        }
    }

    public class CategoryDropdownItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProductManagementItemModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}

