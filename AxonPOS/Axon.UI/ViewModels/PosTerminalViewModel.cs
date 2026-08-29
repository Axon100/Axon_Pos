using Axon.UI.Helpers;
using Axon.UI.Views;
using Axon.UI.ViewModels.Base;
using Axon.Application.Interfaces.Services;
using Axon.Domain.Entities;
using Axon.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using Axon.Application.Interfaces.Repositories;

namespace Axon.UI.ViewModels
{
    public partial class PosTerminalViewModel : BaseViewModel
    {
        private readonly ISalesService _salesService;
        private readonly IPrintService _printService;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<SaleLineItem> _saleLineItemRepository;

        [ObservableProperty]
        private decimal _subtotal;

        [ObservableProperty]
        private decimal _tax;

        [ObservableProperty]
        private decimal _discount;

        public decimal Total => Math.Max(0, Subtotal + Tax - Discount);

        // Discount Modal Properties
        [ObservableProperty]
        private bool _isDiscountDialogOpen;

        [ObservableProperty]
        private decimal _discountInput;

        [ObservableProperty]
        private bool _isDiscountPercentage = false;

        [ObservableProperty]
        private string _discountValidationError = string.Empty;

        [ObservableProperty]
        private bool _hasDiscountError = false;

        // Receipt Modal Properties
        [ObservableProperty]
        private bool _isReceiptDialogOpen;

        [ObservableProperty]
        private int _receiptSaleId;

        [ObservableProperty]
        private DateTime _receiptDate = DateTime.Now;

        [ObservableProperty]
        private decimal _receiptSubtotal;

        [ObservableProperty]
        private decimal _receiptTax;

        [ObservableProperty]
        private decimal _receiptDiscount;

        [ObservableProperty]
        private decimal _receiptTotal;

        [ObservableProperty]
        private string _receiptPaymentMethod = "نقداً (Cash)";

        // ==================== PAYMENT METHOD DIALOG PROPERTIES ====================
        [ObservableProperty]
        private bool _isPaymentMethodDialogOpen;

        [ObservableProperty]
        private string _selectedPaymentMethod = "نقداً (Cash)";

        // ==================== MULTI-INVOICE TAB PROPERTIES ====================
        private readonly PosOrderTabState[] _orderTabs = new PosOrderTabState[]
        {
            new() { TabIndex = 1 },
            new() { TabIndex = 2 },
            new() { TabIndex = 3 }
        };

        [ObservableProperty]
        private int _activeOrderTab = 1;

        [ObservableProperty]
        private bool _isTab1Active = true;

        [ObservableProperty]
        private bool _isTab2Active = false;

        [ObservableProperty]
        private bool _isTab3Active = false;

        [ObservableProperty]
        private string _tab1Badge = string.Empty;

        [ObservableProperty]
        private string _tab2Badge = string.Empty;

        [ObservableProperty]
        private string _tab3Badge = string.Empty;

        // ==================== RETURN / REFUND PROPERTIES ====================
        [ObservableProperty]
        private bool _isReturnDialogOpen;

        [ObservableProperty]
        private string _returnInvoiceSearchQuery = string.Empty;

        [ObservableProperty]
        private string _returnReason = "إرجاع منتج بناءً على طلب العميل";

        [ObservableProperty]
        private bool _returnRestockToInventory = true;

        [ObservableProperty]
        private decimal _totalRefundAmount;

        [ObservableProperty]
        private Sale? _selectedSaleForReturn;

        [ObservableProperty]
        private bool _isSaleFoundForReturn;

        [ObservableProperty]
        private string _returnSearchStatusMessage = string.Empty;

        public ObservableCollection<ReturnItemDisplayModel> ReturnLineItems { get; } = new();
        public ObservableCollection<SaleSummaryItemModel> RecentSalesForReturn { get; } = new();

        public ObservableCollection<CartItem> ReceiptItems { get; } = new();
        public ObservableCollection<ProductItem> Products { get; } = new();
        public ObservableCollection<CartItem> Cart { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<CategoryDisplayItem> CategoryList { get; } = new();

        // Barcode scanner + category filter
        [ObservableProperty]
        private string _barcodeInput = string.Empty;

        [ObservableProperty]
        private string _lastScannedBarcodeFeedback = string.Empty;

        [ObservableProperty]
        private bool _isLastScanSuccess = true;

        [ObservableProperty]
        private bool _hasBarcodeFeedback = false;

        [ObservableProperty]
        private string _selectedCategory = string.Empty;

        [ObservableProperty]
        private CategoryDisplayItem? _selectedCategoryItem;

        partial void OnSelectedCategoryChanged(string value)
        {
            ApplyFilter();
        }

        partial void OnSelectedCategoryItemChanged(CategoryDisplayItem? value)
        {
            ApplyFilter();
        }

        partial void OnBarcodeInputChanged(string value)
        {
            ApplyFilter();
        }

        private List<ProductItem> _allProductItems = new();

        public PosTerminalViewModel(
            ISalesService salesService, 
            IPrintService printService, 
            IRepository<Product> productRepository, 
            IRepository<Category> categoryRepository,
            IRepository<Sale> saleRepository,
            IRepository<SaleLineItem> saleLineItemRepository)
        {
            _salesService = salesService;
            _printService = printService;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _saleRepository = saleRepository;
            _saleLineItemRepository = saleLineItemRepository;
            
            Title = AppResources.GetString("PosTerminal", "نقطة البيع");
            
            _ = LoadDataAsync();
        }

        partial void OnDiscountInputChanged(decimal value)
        {
            ValidateDiscount();
        }

        partial void OnIsDiscountPercentageChanged(bool value)
        {
            ValidateDiscount();
        }

        private void ValidateDiscount()
        {
            if (DiscountInput < 0)
            {
                DiscountValidationError = "غير مسموح بقيم سالبة!";
                HasDiscountError = true;
                return;
            }

            if (DiscountInput > 30m)
            {
                DiscountValidationError = "غير مسموح حدود الخصم 30 جنيه كحد أقصى";
                HasDiscountError = true;
            }
            else
            {
                DiscountValidationError = string.Empty;
                HasDiscountError = false;
            }
        }

        [RelayCommand]
        private void OpenDiscountDialog()
        {
            DiscountInput = Discount;
            DiscountValidationError = string.Empty;
            HasDiscountError = false;
            IsDiscountDialogOpen = true;
        }

        [RelayCommand]
        private void CloseDiscountDialog()
        {
            IsDiscountDialogOpen = false;
        }

        [RelayCommand]
        private void ApplyDiscount()
        {
            ValidateDiscount();
            if (HasDiscountError) return;

            Discount = Math.Min(30m, Math.Max(0m, DiscountInput));
            RecalculateTotals();
            IsDiscountDialogOpen = false;
        }

        // ==================== PAYMENT METHOD COMMANDS ====================

        [RelayCommand]
        private void OpenPaymentMethodDialog()
        {
            IsPaymentMethodDialogOpen = true;
        }

        [RelayCommand]
        private void ClosePaymentMethodDialog()
        {
            IsPaymentMethodDialogOpen = false;
        }

        [RelayCommand]
        private void SelectPaymentMethod(string method)
        {
            if (!string.IsNullOrWhiteSpace(method))
            {
                SelectedPaymentMethod = method;
            }
            IsPaymentMethodDialogOpen = false;
        }

        // ==================== MULTI-INVOICE TAB COMMANDS ====================

        [RelayCommand]
        private void SwitchOrderTab(string tabParam)
        {
            if (int.TryParse(tabParam, out int newTab) && newTab >= 1 && newTab <= 3)
            {
                if (newTab == ActiveOrderTab) return;

                // 1. Save current active tab state
                var currentSlot = _orderTabs[ActiveOrderTab - 1];
                currentSlot.Items = Cart.Select(item => new CartItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Sku = item.Sku,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    IsTaxable = item.IsTaxable,
                    TaxAmount = item.TaxAmount
                }).ToList();
                currentSlot.Discount = Discount;
                currentSlot.PaymentMethod = SelectedPaymentMethod;

                // 2. Change active tab index
                ActiveOrderTab = newTab;
                IsTab1Active = (ActiveOrderTab == 1);
                IsTab2Active = (ActiveOrderTab == 2);
                IsTab3Active = (ActiveOrderTab == 3);

                // 3. Load target tab state
                var targetSlot = _orderTabs[ActiveOrderTab - 1];
                Cart.Clear();
                foreach (var item in targetSlot.Items)
                {
                    Cart.Add(new CartItem
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Sku = item.Sku,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        IsTaxable = item.IsTaxable,
                        TaxAmount = item.TaxAmount
                    });
                }
                Discount = targetSlot.Discount;
                SelectedPaymentMethod = targetSlot.PaymentMethod;

                // 4. Refresh badges and totals
                RecalculateTotals();
                UpdateTabBadges();
            }
        }

        public void UpdateTabBadges()
        {
            if (ActiveOrderTab >= 1 && ActiveOrderTab <= 3)
            {
                var currentSlot = _orderTabs[ActiveOrderTab - 1];
                currentSlot.Items = Cart.Select(item => new CartItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Sku = item.Sku,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    IsTaxable = item.IsTaxable,
                    TaxAmount = item.TaxAmount
                }).ToList();
                currentSlot.Discount = Discount;
                currentSlot.PaymentMethod = SelectedPaymentMethod;
            }

            Tab1Badge = _orderTabs[0].HasItems ? $"({_orderTabs[0].ItemsCount})" : string.Empty;
            Tab2Badge = _orderTabs[1].HasItems ? $"({_orderTabs[1].ItemsCount})" : string.Empty;
            Tab3Badge = _orderTabs[2].HasItems ? $"({_orderTabs[2].ItemsCount})" : string.Empty;
        }

        // ==================== RETURN WORKFLOW COMMANDS ====================

        [ObservableProperty]
        private SaleSummaryItemModel? _selectedRecentSaleItem;

        partial void OnSelectedRecentSaleItemChanged(SaleSummaryItemModel? value)
        {
            if (value != null)
            {
                _ = SelectRecentSaleForReturnAsync(value);
            }
        }

        [RelayCommand]
        private async Task OpenReturnDialogAsync()
        {
            ReturnInvoiceSearchQuery = string.Empty;
            ReturnReason = "إرجاع منتج بناءً على طلب العميل";
            ReturnRestockToInventory = true;
            TotalRefundAmount = 0;
            SelectedSaleForReturn = null;
            IsSaleFoundForReturn = false;
            ReturnSearchStatusMessage = string.Empty;
            ReturnLineItems.Clear();
            RecentSalesForReturn.Clear();
            SelectedRecentSaleItem = null;

            await LoadRecentSalesListAsync();
            IsReturnDialogOpen = true;
        }

        private async Task LoadRecentSalesListAsync()
        {
            try
            {
                RecentSalesForReturn.Clear();
                var allSales = await _saleRepository.GetAllAsync();
                var allLineItems = await _saleLineItemRepository.GetAllAsync();
                var recent = allSales.OrderByDescending(s => s.Date).Take(20).ToList();
                foreach (var s in recent)
                {
                    RecentSalesForReturn.Add(new SaleSummaryItemModel
                    {
                        Id = s.Id,
                        ReceiptNumber = s.ReceiptNumber,
                        Date = s.Date,
                        Total = s.Total,
                        Status = s.Status,
                        ItemsCount = allLineItems.Count(li => li.SaleId == s.Id)
                    });
                }

                if (RecentSalesForReturn.Count > 0 && SelectedSaleForReturn == null)
                {
                    SelectedRecentSaleItem = RecentSalesForReturn[0];
                }
            }
            catch
            {
                // Ignore silent load error
            }
        }

        [RelayCommand]
        private void CloseReturnDialog()
        {
            IsReturnDialogOpen = false;
        }

        [RelayCommand]
        private async Task SearchSaleForReturnAsync()
        {
            if (string.IsNullOrWhiteSpace(ReturnInvoiceSearchQuery))
            {
                await LoadRecentSalesListAsync();
                return;
            }

            var query = ReturnInvoiceSearchQuery.Trim();
            IsBusy = true;
            ReturnSearchStatusMessage = string.Empty;

            try
            {
                var allSales = (await _saleRepository.GetAllAsync()).OrderByDescending(s => s.Date).ToList();
                var allLineItems = await _saleLineItemRepository.GetAllAsync();
                var allProducts = await _productRepository.GetAllAsync();

                // 1. Direct match on ReceiptNumber, ID, Total, or Date
                var matchedSales = allSales.Where(s => 
                    s.ReceiptNumber.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Id.ToString() == query ||
                    s.Total.ToString("0.00").Contains(query) ||
                    s.Date.ToString("yyyy-MM-dd").Contains(query) ||
                    s.Date.ToString("dd/MM/yyyy").Contains(query)).ToList();

                // 2. Product Name / SKU / Barcode match inside invoice
                if (matchedSales.Count == 0)
                {
                    var matchedProducts = allProducts.Where(p => 
                        (p.NameAR != null && p.NameAR.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        (p.NameEN != null && p.NameEN.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        (p.SKU != null && p.SKU.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Barcode != null && p.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase))).Select(p => p.Id).ToHashSet();

                    if (matchedProducts.Count > 0)
                    {
                        var saleIdsWithProduct = allLineItems
                            .Where(li => matchedProducts.Contains(li.ProductId))
                            .Select(li => li.SaleId)
                            .Distinct()
                            .ToHashSet();

                        matchedSales = allSales.Where(s => saleIdsWithProduct.Contains(s.Id)).ToList();
                    }
                }

                if (matchedSales.Count == 0)
                {
                    IsSaleFoundForReturn = false;
                    SelectedSaleForReturn = null;
                    ReturnLineItems.Clear();
                    ReturnSearchStatusMessage = $"لم يتم العثور على أي فاتورة مطابقة لـ '{query}'!";
                    return;
                }

                // Update the found list with matching sales
                RecentSalesForReturn.Clear();
                foreach (var s in matchedSales.Take(25))
                {
                    RecentSalesForReturn.Add(new SaleSummaryItemModel
                    {
                        Id = s.Id,
                        ReceiptNumber = s.ReceiptNumber,
                        Date = s.Date,
                        Total = s.Total,
                        Status = s.Status,
                        ItemsCount = allLineItems.Count(li => li.SaleId == s.Id)
                    });
                }

                var firstSale = matchedSales[0];
                await LoadSaleLineItemsForReturnAsync(firstSale);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SelectRecentSaleForReturnAsync(SaleSummaryItemModel item)
        {
            if (item == null) return;
            IsBusy = true;
            try
            {
                var sale = await _saleRepository.GetByIdAsync(item.Id);
                if (sale != null)
                {
                    await LoadSaleLineItemsForReturnAsync(sale);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSaleLineItemsForReturnAsync(Sale sale)
        {
            SelectedSaleForReturn = sale;
            IsSaleFoundForReturn = true;
            ReturnSearchStatusMessage = string.Empty;
            ReturnLineItems.Clear();

            // Load line items with product details
            var allLines = await _saleLineItemRepository.GetAsync(x => x.SaleId == sale.Id);
            var products = await _productRepository.GetAllAsync();
            var productMap = products.ToDictionary(p => p.Id);

            // Calculate discount factor for proportional refund
            var invoiceGrossSubtotal = allLines.Sum(x => x.Quantity * x.UnitPrice);
            var discountFactor = (invoiceGrossSubtotal > 0 && sale.DiscountAmount > 0)
                ? Math.Max(0, (invoiceGrossSubtotal - sale.DiscountAmount) / invoiceGrossSubtotal)
                : 1.0m;

            foreach (var line in allLines)
            {
                productMap.TryGetValue(line.ProductId, out var prod);
                var productName = prod != null ? (string.IsNullOrEmpty(prod.NameAR) ? prod.NameEN : prod.NameAR) : $"منتج #{line.ProductId}";
                var sku = prod?.SKU ?? string.Empty;

                var effectivePrice = Math.Round(line.UnitPrice * discountFactor, 2);

                var returnItem = new ReturnItemDisplayModel
                {
                    SaleLineItemId = line.Id,
                    ProductId = line.ProductId,
                    ProductName = productName,
                    Sku = sku,
                    UnitPrice = line.UnitPrice,
                    EffectiveUnitPrice = effectivePrice,
                    MaxQuantity = line.Quantity,
                    QuantityToReturn = line.Quantity // Default to returning all
                };

                returnItem.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ReturnItemDisplayModel.QuantityToReturn) || e.PropertyName == nameof(ReturnItemDisplayModel.LineRefundTotal))
                    {
                        RecalculateRefundTotal();
                    }
                };

                ReturnLineItems.Add(returnItem);
            }

            RecalculateRefundTotal();
        }

        [RelayCommand]
        private void IncreaseReturnQuantity(ReturnItemDisplayModel item)
        {
            if (item == null) return;
            if (item.QuantityToReturn < item.MaxQuantity)
            {
                item.QuantityToReturn++;
                RecalculateRefundTotal();
            }
        }

        [RelayCommand]
        private void DecreaseReturnQuantity(ReturnItemDisplayModel item)
        {
            if (item == null) return;
            if (item.QuantityToReturn > 0)
            {
                item.QuantityToReturn--;
                RecalculateRefundTotal();
            }
        }

        [RelayCommand]
        private void SetFullReturnQuantity(ReturnItemDisplayModel item)
        {
            if (item == null) return;
            item.QuantityToReturn = item.MaxQuantity;
            RecalculateRefundTotal();
        }

        [RelayCommand]
        private void ClearReturnQuantity(ReturnItemDisplayModel item)
        {
            if (item == null) return;
            item.QuantityToReturn = 0;
            RecalculateRefundTotal();
        }

        private void RecalculateRefundTotal()
        {
            TotalRefundAmount = ReturnLineItems.Sum(x => x.LineRefundTotal);
        }

        [RelayCommand]
        private async Task ProcessReturnAsync()
        {
            if (SelectedSaleForReturn == null)
            {
                AxonMessageBox.Show("يرجى اختيار الفاتورة المراد إرجاعها أولاً!", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var itemsToReturn = ReturnLineItems.Where(x => x.QuantityToReturn > 0).ToList();
            if (itemsToReturn.Count == 0)
            {
                AxonMessageBox.Show("يرجى تحديد كمية أكبر من صفر للأصناف المراد إرجاعها!", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!UserSessionService.HasPermission("POS.Sell"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لإجراء عمليات المرتجع في نقطة البيع!", "تنبيه الصلاحيات (RBAC)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var totalReturnedQty = itemsToReturn.Sum(x => x.QuantityToReturn);
            var confirm = AxonMessageBox.Show(
                $"تأكيد إرجاع ({totalReturnedQty}) قطعة بقيمة إجمالية {TotalRefundAmount:N2} ج.م؟\nسيتم إضافة الكميات المرتجعة تلقائياً إلى المخزون.",
                "تأكيد عملية المرتجع",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var returnEntity = new Return
                {
                    SaleId = SelectedSaleForReturn.Id,
                    UserId = UserSessionService.CurrentUserId > 0 ? UserSessionService.CurrentUserId : 1,
                    ReturnDate = DateTimeOffset.Now,
                    TotalRefundAmount = TotalRefundAmount,
                    Reason = string.IsNullOrWhiteSpace(ReturnReason) ? "مرتجع من نقطة البيع" : ReturnReason,
                    ReturnLineItems = itemsToReturn.Select(item => new ReturnLineItem
                    {
                        SaleLineItemId = item.SaleLineItemId,
                        QuantityReturned = (int)item.QuantityToReturn,
                        RefundAmount = item.LineRefundTotal,
                        RestockToInventory = ReturnRestockToInventory
                    }).ToList()
                };

                await _salesService.ProcessReturnAsync(returnEntity);

                // Reload product inventory to reflect updated stock instantly
                await LoadDataAsync();

                IsReturnDialogOpen = false;

                AxonMessageBox.Show(
                    $"تمت عملية المرتجع بنجاح! تم استرجاع {TotalRefundAmount:N2} ج.م وإعادة {totalReturnedQty} قطعة إلى المخزون.",
                    "نجاح المرتجع",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AxonMessageBox.Show($"فشل تنفيذ المرتجع: {ex.Message}", "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== RECEIPT ACTIONS ====================

        [RelayCommand]
        private void CloseReceiptDialog()
        {
            IsReceiptDialogOpen = false;
        }

        [RelayCommand]
        private async Task PrintReceiptAsync()
        {
            if (ReceiptSaleId > 0)
            {
                try
                {
                    await _printService.PrintReceiptAsync(ReceiptSaleId);
                }
                catch (Exception ex)
                {
                    AxonMessageBox.Show($"تعذر إرسال أمر الطباعة: {ex.Message}", "تنبيه الطباعة", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
        }

        // ==================== POS CATALOG & CART ====================

        private readonly SemaphoreSlim _loadDataLock = new(1, 1);

        public async Task LoadDataAsync()
        {
            if (!await _loadDataLock.WaitAsync(100)) return; // Prevent concurrent re-entry
            IsBusy = true;
            try
            {
                Categories.Clear();
                CategoryList.Clear();
                Categories.Add(AppResources.GetString("AllCategories", "جميع الأصناف"));
                
                var allCategories = await _categoryRepository.GetAllAsync();
                var allProducts = await _productRepository.GetAllAsync();
                var productList = allProducts.ToList();

                CategoryList.Add(new CategoryDisplayItem
                {
                    Id = null,
                    Name = AppResources.GetString("AllCategories", "جميع الأصناف"),
                    ProductCount = productList.Count,
                    IconKind = "ViewGridOutline"
                });

                foreach (var c in allCategories)
                {
                    var catName = string.IsNullOrEmpty(c.NameAR) ? c.NameEN : c.NameAR;
                    if (!string.IsNullOrEmpty(catName))
                    {
                        Categories.Add(catName);
                        var count = productList.Count(p => p.CategoryId == c.Id);
                        CategoryList.Add(new CategoryDisplayItem
                        {
                            Id = c.Id,
                            Name = catName,
                            ProductCount = count,
                            IconKind = "TagOutline"
                        });
                    }
                }

                _allProductItems.Clear();
                foreach (var p in productList)
                {
                    var category = allCategories.FirstOrDefault(c => c.Id == p.CategoryId);
                    var catName = category != null ? (string.IsNullOrEmpty(category.NameAR) ? category.NameEN : category.NameAR) : "General";

                    _allProductItems.Add(new ProductItem
                    {
                        Id = p.Id,
                        Name = string.IsNullOrEmpty(p.NameAR) ? (string.IsNullOrEmpty(p.NameEN) ? $"صنف #{p.Id}" : p.NameEN) : p.NameAR,
                        Sku = p.SKU ?? string.Empty,
                        Barcode = p.Barcode ?? string.Empty,
                        Price = p.SellingPrice,
                        Stock = (int)p.CurrentStock,
                        Category = catName,
                        CategoryId = p.CategoryId,
                        ImageUrl = string.IsNullOrEmpty(p.ImagePath) ? null : p.ImagePath,
                        IsTaxable = p.IsTaxable,
                        TaxAmount = p.TaxAmount
                    });
                }

                if (SelectedCategoryItem == null && CategoryList.Count > 0)
                {
                    SelectedCategoryItem = CategoryList[0];
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PosTerminalViewModel.LoadDataAsync] Exception: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                _loadDataLock.Release();
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allProductItems.AsEnumerable();

            if (SelectedCategoryItem != null && SelectedCategoryItem.Id.HasValue)
            {
                filtered = filtered.Where(p => p.CategoryId == SelectedCategoryItem.Id.Value);
            }
            else if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != AppResources.GetString("AllCategories", "جميع الأصناف"))
            {
                filtered = filtered.Where(p => p.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(BarcodeInput))
            {
                var query = BarcodeInput.Trim();
                filtered = filtered.Where(p => 
                    (p.Barcode != null && p.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Sku != null && p.Sku.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Name != null && p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)));
            }

            Products.Clear();
            foreach (var item in filtered)
            {
                Products.Add(item);
            }
        }

        [RelayCommand]
        private void ClearBarcodeInput()
        {
            BarcodeInput = string.Empty;
            HasBarcodeFeedback = false;
        }

        [RelayCommand]
        private void ProcessBarcodeInput()
        {
            if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

            var query = BarcodeInput.Trim();
            var matchedProduct = _allProductItems.FirstOrDefault(p => 
                (p.Barcode != null && p.Barcode.Equals(query, StringComparison.OrdinalIgnoreCase)) ||
                (p.Barcode != null && p.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase)));

            if (matchedProduct != null)
            {
                AddToCart(matchedProduct);
                LastScannedBarcodeFeedback = $"✓ تم مسح الباركود بنجاح: [{query}] — تمت إضافة ({matchedProduct.Name}) بسعر {matchedProduct.Price:#,##0.##} ج.م للسلة!";
                IsLastScanSuccess = true;
                HasBarcodeFeedback = true;
                BarcodeInput = string.Empty;
            }
            else
            {
                LastScannedBarcodeFeedback = $"⚠️ الباركود أو الكود [{query}] غير مسجل في المنتجات!";
                IsLastScanSuccess = false;
                HasBarcodeFeedback = true;
                ApplyFilter();
            }
        }

        [RelayCommand]
        private void AddToCart(ProductItem product)
        {
            if (product == null) return;

            var existing = Cart.FirstOrDefault(x => x.Id == product.Id);
            if (existing != null)
            {
                if (existing.Quantity < product.Stock)
                {
                    existing.Quantity++;
                }
                else
                {
                    AxonMessageBox.Show("لا توجد كمية كافية بالمخزون لهذا المنتج!", "تنبيه المخزون", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            else
            {
                if (product.Stock > 0)
                {
                    Cart.Add(new CartItem
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Sku = product.Sku,
                        Price = product.Price,
                        Quantity = 1,
                        IsTaxable = product.IsTaxable,
                        TaxAmount = product.TaxAmount
                    });
                }
                else
                {
                    AxonMessageBox.Show("هذا المنتج غير متوفر حالياً بالمخزون!", "تنبيه المخزون", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            RecalculateTotals();
        }

        [RelayCommand]
        private void IncreaseQuantity(CartItem item)
        {
            if (item == null) return;
            var product = _allProductItems.FirstOrDefault(p => p.Id == item.Id);
            if (product != null && item.Quantity < product.Stock)
            {
                item.Quantity++;
                RecalculateTotals();
            }
            else
            {
                AxonMessageBox.Show("وصلت إلى الحد الأقصى للكمية المتاحة في المخزون!", "تنبيه المخزون", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void DecreaseQuantity(CartItem item)
        {
            if (item == null) return;
            if (item.Quantity > 1)
            {
                item.Quantity--;
            }
            else
            {
                Cart.Remove(item);
            }
            RecalculateTotals();
        }

        [RelayCommand]
        private void RemoveFromCart(CartItem item)
        {
            if (item != null)
            {
                Cart.Remove(item);
            }
            RecalculateTotals();
        }

        [RelayCommand]
        private void ClearCart()
        {
            Cart.Clear();
            RecalculateTotals();
        }

        public bool IsTaxVisible => Tax > 0;

        private void RecalculateTotals()
        {
            Subtotal = Cart.Sum(x => x.TotalPrice);
            Tax = Cart.Where(x => x.IsTaxable).Sum(x => x.Quantity * x.TaxAmount);
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(IsTaxVisible));
            UpdateTabBadges();
        }

        [RelayCommand]
        private async Task CheckoutAsync()
        {
            if (IsBusy || Cart.Count == 0) return;

            if (!UserSessionService.HasPermission("POS.Sell"))
            {
                AxonMessageBox.Show("ليس لديك صلاحية لإتمام وطباعة عمليات البيع!", "تنبيه الصلاحيات (RBAC)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var chosenMethod = string.IsNullOrWhiteSpace(SelectedPaymentMethod) ? "نقداً (Cash)" : SelectedPaymentMethod;

                var sale = new Sale
                {
                    ReceiptNumber = await _salesService.GenerateInvoiceNumberAsync(),
                    Date = DateTime.Now,
                    CashierId = UserSessionService.CurrentUserId > 0 ? UserSessionService.CurrentUserId : 1,
                    SubTotal = Subtotal,
                    TaxAmount = Tax,
                    DiscountAmount = Discount,
                    Status = "Completed",
                    LineItems = Cart.Select(c => new SaleLineItem
                    {
                        ProductId = c.Id,
                        Quantity = c.Quantity,
                        UnitPrice = c.Price
                    }).ToList(),
                    Payments = new List<Payment>
                    {
                        new Payment
                        {
                            PaymentMethod = chosenMethod,
                            Amount = Total,
                            PaymentDate = DateTime.Now
                        }
                    }
                };

                var completedSale = await _salesService.ProcessSaleAsync(sale);

                // Auto-print receipt
                try
                {
                    await _printService.PrintReceiptAsync(completedSale.Id);
                }
                catch
                {
                    // If no physical printer configured, ignore print failure and continue flow
                }

                // Show Receipt Modal
                ReceiptSaleId = completedSale.Id;
                ReceiptDate = completedSale.Date;
                ReceiptSubtotal = Subtotal;
                ReceiptTax = Tax;
                ReceiptDiscount = Discount;
                ReceiptTotal = Total;
                ReceiptPaymentMethod = chosenMethod;

                ReceiptItems.Clear();
                foreach (var item in Cart)
                {
                    ReceiptItems.Add(new CartItem
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Sku = item.Sku,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        IsTaxable = item.IsTaxable
                    });
                }

                IsReceiptDialogOpen = true;

                // Reload product inventory to reflect deducted stock
                await LoadDataAsync();

                // Clear current active slot & POS Cart
                var currentSlot = _orderTabs[ActiveOrderTab - 1];
                currentSlot.Items.Clear();
                currentSlot.Discount = 0;
                currentSlot.PaymentMethod = "نقداً (Cash)";
                Discount = 0;
                SelectedPaymentMethod = "نقداً (Cash)";
                Cart.Clear();
                RecalculateTotals();
                UpdateTabBadges();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class PosOrderTabState
    {
        public int TabIndex { get; set; }
        public List<CartItem> Items { get; set; } = new();
        public decimal Discount { get; set; } = 0;
        public string PaymentMethod { get; set; } = "نقداً (Cash)";

        public int ItemsCount => Items.Sum(x => (int)x.Quantity);
        public bool HasItems => Items.Count > 0;
    }

    public class CategoryDisplayItem : ObservableObject
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public string IconKind { get; set; } = "TagOutline";
    }

    public class ProductItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsTaxable { get; set; } = false;
        public decimal TaxAmount { get; set; } = 0;

        public bool IsOutOfStock => Stock <= 0;
        public bool IsLowStock => Stock > 0 && Stock < 10;
        public string StockDisplay => $"{Stock} متوفر";
    }

    public partial class CartItem : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }

        [ObservableProperty]
        private decimal _quantity;

        public bool IsTaxable { get; set; } = false;
        public decimal TaxAmount { get; set; } = 0;

        public decimal TotalPrice => Quantity * Price;

        partial void OnQuantityChanged(decimal value)
        {
            OnPropertyChanged(nameof(TotalPrice));
        }
    }

    public partial class ReturnItemDisplayModel : ObservableObject
    {
        public int SaleLineItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal EffectiveUnitPrice { get; set; }
        public decimal DiscountPerUnit => Math.Max(0, UnitPrice - EffectiveUnitPrice);
        public decimal MaxQuantity { get; set; }

        [ObservableProperty]
        private decimal _quantityToReturn;

        public decimal LineRefundTotal => QuantityToReturn * EffectiveUnitPrice;

        partial void OnQuantityToReturnChanged(decimal value)
        {
            if (value < 0) QuantityToReturn = 0;
            if (value > MaxQuantity) QuantityToReturn = MaxQuantity;
            OnPropertyChanged(nameof(LineRefundTotal));
        }
    }

    public class SaleSummaryItemModel
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
    }
}
