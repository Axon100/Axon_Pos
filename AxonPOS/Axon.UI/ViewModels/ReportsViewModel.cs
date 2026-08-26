using Axon.Application.Interfaces.Repositories;
using Axon.Domain.Entities;
using Axon.UI.Helpers;
using Axon.UI.ViewModels.Base;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Axon.UI.ViewModels
{
    public partial class ReportsViewModel : BaseViewModel
    {
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<SaleLineItem> _lineItemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<Return> _returnRepository;

        // ===== 4 Main Report Categories (Tabs) =====
        // 0: الصندوق المغلق (Closed Register)
        // 1: تصنيف المبيعات (Category Sales)
        // 2: مبيعات منتج موسع (Extended Product Sales)
        // 3: سجل الفواتير (Sales Invoices)
        [ObservableProperty]
        private int _selectedReportTab = 0;

        // ===== Date Range Filter =====
        [ObservableProperty]
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today;

        // ===== Category & Product Filter Dropdowns =====
        public ObservableCollection<CategoryOptionModel> FilterCategories { get; } = new();
        public ObservableCollection<ProductFilterOptionModel> FilterProducts { get; } = new();

        private List<ProductFilterOptionModel> _allProductsCache = new();
        private List<Product> _productsEntityCache = new();

        [ObservableProperty]
        private CategoryOptionModel? _selectedFilterCategory;

        [ObservableProperty]
        private ProductFilterOptionModel? _selectedFilterProduct;

        [ObservableProperty]
        private string _reportStatus = "اختر الفترة الزمنية ثم اضغط 'إنشاء التقرير'";

        [ObservableProperty]
        private bool _hasReportData = false;

        // ===== Closed Box (الصندوق المغلق - Tab 0) KPIs =====
        [ObservableProperty]
        private decimal _closedBoxCashTotal;

        [ObservableProperty]
        private decimal _closedBoxReturnTotal;

        [ObservableProperty]
        private decimal _closedBoxNetTotal;

        [ObservableProperty]
        private int _closedBoxClosuresCount;

        [ObservableProperty]
        private int _closedBoxInvoicesCount;

        // ===== Category Sales Classification (تصنيف المبيعات - Tab 1) KPIs =====
        [ObservableProperty]
        private decimal _categoryReportTotalSales;

        [ObservableProperty]
        private int _categoryReportTotalQty;

        [ObservableProperty]
        private string _categoryReportTopCategory = "—";

        [ObservableProperty]
        private int _categoryReportActiveCount;

        // ===== Extended Product Sales (مبيعات منتج موسع - Tab 2) KPIs =====
        [ObservableProperty]
        private decimal _productReportTotalSales;

        [ObservableProperty]
        private int _productReportTotalQty;

        [ObservableProperty]
        private string _productReportTopProduct = "—";

        [ObservableProperty]
        private int _productReportActiveCount;

        // ===== Single Product Cycle Mode (تفاصيل دورة مبيعات منتج محدد) =====
        [ObservableProperty]
        private bool _isSingleProductMode = false;

        [ObservableProperty]
        private string _singleProductName = string.Empty;

        [ObservableProperty]
        private string _singleProductSku = string.Empty;

        [ObservableProperty]
        private string _singleProductCategory = string.Empty;

        [ObservableProperty]
        private decimal _singleProductPrice;

        [ObservableProperty]
        private int _singleProductCurrentStock;

        // ===== General & Period Metadata =====
        [ObservableProperty]
        private string _reportPeriodTitle = string.Empty;

        [ObservableProperty]
        private string _selectedCategoryTitle = "كافة الأقسام والتصنيفات";

        [ObservableProperty]
        private decimal _totalRevenue;

        [ObservableProperty]
        private decimal _totalExpenses;

        [ObservableProperty]
        private decimal _totalReturns;

        [ObservableProperty]
        private decimal _totalDiscounts;

        [ObservableProperty]
        private decimal _totalTax;

        [ObservableProperty]
        private decimal _netProfit;

        // ===== Toolbar State =====
        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private double _zoomScale = 1.0;

        [ObservableProperty]
        private string _zoomText = "100%";

        // ===== Collections =====
        public ObservableCollection<ClosedRegisterReportItem> ClosedRegisterReport { get; } = new();
        public ObservableCollection<SalesClassificationReportItem> SalesClassificationReport { get; } = new();
        public ObservableCollection<ProductSalesReportItem> ProductSalesReport { get; } = new();
        public ObservableCollection<ProductCycleTransactionItem> SingleProductCycleReport { get; } = new();
        public ObservableCollection<SalesClosingReportItem> SalesInvoicesReport { get; } = new();

        public ReportsViewModel(
            IRepository<Sale> saleRepository,
            IRepository<SaleLineItem> lineItemRepository,
            IRepository<Product> productRepository,
            IRepository<Category> categoryRepository,
            IRepository<User> userRepository,
            IRepository<Expense> expenseRepository,
            IRepository<Return> returnRepository)
        {
            _saleRepository = saleRepository;
            _lineItemRepository = lineItemRepository;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;
            _expenseRepository = expenseRepository;
            _returnRepository = returnRepository;

            Title = "التقارير والتحليلات";
            _ = LoadFiltersAsync();
        }

        private async Task LoadFiltersAsync()
        {
            try
            {
                var cats = await _categoryRepository.GetAllAsync();
                _productsEntityCache = (await _productRepository.GetAllAsync()).ToList();

                FilterCategories.Clear();
                FilterCategories.Add(new CategoryOptionModel { Id = 0, Name = "كافة الأقسام" });
                foreach (var c in cats)
                {
                    FilterCategories.Add(new CategoryOptionModel
                    {
                        Id = c.Id,
                        Name = string.IsNullOrEmpty(c.NameAR) ? c.NameEN : c.NameAR
                    });
                }
                SelectedFilterCategory = FilterCategories.FirstOrDefault();

                _allProductsCache = _productsEntityCache.Select(p => new ProductFilterOptionModel
                {
                    Id = p.Id,
                    Name = string.IsNullOrEmpty(p.NameAR) ? p.NameEN : p.NameAR,
                    Sku = p.SKU ?? string.Empty,
                    CategoryId = p.CategoryId
                }).ToList();

                RefreshProductsFilter();
            }
            catch { }
        }

        private void RefreshProductsFilter()
        {
            FilterProducts.Clear();
            FilterProducts.Add(new ProductFilterOptionModel { Id = 0, Name = "كافة الأصناف والمنتجات" });

            var catId = SelectedFilterCategory?.Id ?? 0;
            var list = catId > 0 ? _allProductsCache.Where(p => p.CategoryId == catId) : _allProductsCache;
            foreach (var p in list)
            {
                FilterProducts.Add(p);
            }
            SelectedFilterProduct = FilterProducts.FirstOrDefault();
        }

        partial void OnSelectedFilterCategoryChanged(CategoryOptionModel? value)
        {
            RefreshProductsFilter();
        }

        [RelayCommand]
        private void SetTab(string? tabIndex)
        {
            if (int.TryParse(tabIndex, out var idx))
            {
                SelectedReportTab = idx;
            }
        }

        // ==================== GENERATE REPORT ====================

        [RelayCommand]
        public async Task GenerateReportAsync()
        {
            if (EndDate < StartDate)
            {
                ReportStatus = "تاريخ النهاية يجب أن يكون بعد تاريخ البداية.";
                return;
            }

            IsBusy = true;
            ReportStatus = "جاري استخراج وتحليل بيانات التقرير من النظام...";
            HasReportData = false;

            try
            {
                var allSales = await _saleRepository.GetAllAsync();
                var allReturns = await _returnRepository.GetAllAsync();
                var allLineItems = await _lineItemRepository.GetAllAsync();
                var allProducts = await _productRepository.GetAllAsync();
                var allCategories = await _categoryRepository.GetAllAsync();
                var allUsers = await _userRepository.GetAllAsync();
                var allExpenses = await _expenseRepository.GetAllAsync();

                _productsEntityCache = allProducts.ToList();

                var start = StartDate.Date;
                var end = EndDate.Date.AddDays(1).AddTicks(-1);

                var filteredSales = allSales.Where(s => s.Date >= start && s.Date <= end).ToList();
                var filteredReturns = allReturns.Where(r => r.ReturnDate.DateTime >= start && r.ReturnDate.DateTime <= end).ToList();
                var filteredExpenses = allExpenses.Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end).ToList();

                var userMap = allUsers.ToDictionary(u => u.Id, u => u.Username);
                var productMap = allProducts.ToDictionary(p => p.Id, p => p);
                var catMap = allCategories.ToDictionary(c => c.Id, c => string.IsNullOrEmpty(c.NameAR) ? c.NameEN : c.NameAR);
                var saleMap = filteredSales.ToDictionary(s => s.Id, s => s);

                var saleIds = filteredSales.Select(s => s.Id).ToHashSet();
                var filteredLineItems = allLineItems.Where(li => saleIds.Contains(li.SaleId)).ToList();

                // Apply Category Filter if a specific category is chosen
                var selectedCatId = SelectedFilterCategory?.Id ?? 0;
                SelectedCategoryTitle = selectedCatId > 0 && SelectedFilterCategory != null
                    ? SelectedFilterCategory.Name
                    : "كافة الأقسام والتصنيفات";

                if (selectedCatId > 0)
                {
                    var matchingProductIds = allProducts.Where(p => p.CategoryId == selectedCatId).Select(p => p.Id).ToHashSet();
                    filteredLineItems = filteredLineItems.Where(li => matchingProductIds.Contains(li.ProductId)).ToList();
                }

                // ==================== 1. الصندوق المغلق (CLOSED REGISTER REPORT - TAB 0) ====================
                ClosedRegisterReport.Clear();

                var salesByDayAndCashier = filteredSales
                    .GroupBy(s => new { Day = s.Date.Date, s.CashierId })
                    .OrderBy(g => g.Key.Day)
                    .ToList();

                int seq = 1;
                decimal totalCash = 0;
                decimal totalReturns = 0;

                if (salesByDayAndCashier.Count > 0)
                {
                    foreach (var group in salesByDayAndCashier)
                    {
                        var groupSales = group.ToList();
                        var cashierName = userMap.TryGetValue(group.Key.CashierId, out var un) ? un : "كاشير عام";
                        
                        var startTime = groupSales.Min(s => s.Date);
                        var endTime = groupSales.Max(s => s.Date);
                        if (startTime == endTime) endTime = startTime.AddHours(8);

                        var groupCashSales = groupSales.Sum(s => s.Total);
                        
                        var groupReturns = filteredReturns
                            .Where(r => r.ReturnDate.Date == group.Key.Day && (r.UserId == group.Key.CashierId || r.UserId == 0))
                            .Sum(r => r.TotalRefundAmount);

                        var netSales = groupCashSales - groupReturns;

                        totalCash += groupCashSales;
                        totalReturns += groupReturns;

                        ClosedRegisterReport.Add(new ClosedRegisterReportItem
                        {
                            SequenceNumber = seq++,
                            TerminalName = "Cash-PC",
                            CashierName = cashierName,
                            StartTime = startTime,
                            EndTime = endTime,
                            CashSales = groupCashSales,
                            ReturnsAmount = groupReturns,
                            GrossSales = groupCashSales,
                            NetSales = netSales,
                            InvoicesCount = groupSales.Count,
                            Status = "مغلق"
                        });
                    }
                }
                else if (filteredReturns.Count > 0)
                {
                    var groupReturns = filteredReturns.Sum(r => r.TotalRefundAmount);
                    totalReturns += groupReturns;
                    ClosedRegisterReport.Add(new ClosedRegisterReportItem
                    {
                        SequenceNumber = seq++,
                        TerminalName = "Cash-PC",
                        CashierName = "Admin",
                        StartTime = start,
                        EndTime = end,
                        CashSales = 0,
                        ReturnsAmount = groupReturns,
                        GrossSales = 0,
                        NetSales = -groupReturns,
                        InvoicesCount = 0,
                        Status = "مغلق"
                    });
                }

                ClosedBoxCashTotal = totalCash;
                ClosedBoxReturnTotal = totalReturns;
                ClosedBoxNetTotal = totalCash - totalReturns;
                ClosedBoxClosuresCount = ClosedRegisterReport.Count;
                ClosedBoxInvoicesCount = filteredSales.Count;
                ReportPeriodTitle = $"الفترة: من {StartDate:yyyy/MM/dd} إلى {EndDate:yyyy/MM/dd}";

                // ==================== 2. تصنيف المبيعات (SALES CLASSIFICATION - TAB 1) ====================
                SalesClassificationReport.Clear();

                var grandCategorySales = filteredLineItems.Sum(li => li.LineTotal);

                var categoryGroups = filteredLineItems.GroupBy(li =>
                {
                    if (productMap.TryGetValue(li.ProductId, out var p))
                    {
                        return p.CategoryId;
                    }
                    return 0;
                }).ToList();

                foreach (var g in categoryGroups)
                {
                    var catId = g.Key;
                    var catName = catMap.TryGetValue(catId, out var cn) ? cn : (catId == 0 ? "بدون قسم" : $"قسم #{catId}");
                    var distinctProducts = g.Select(li => li.ProductId).Distinct().Count();
                    var qtySold = (int)g.Sum(li => li.Quantity);
                    var totalSales = g.Sum(li => li.LineTotal);
                    var pct = grandCategorySales > 0 ? (double)(totalSales / grandCategorySales * 100) : 0;

                    SalesClassificationReport.Add(new SalesClassificationReportItem
                    {
                        CategoryId = catId,
                        CategoryName = catName,
                        DistinctProductsCount = distinctProducts,
                        QuantitySold = qtySold,
                        TotalSales = totalSales,
                        Percentage = pct
                    });
                }

                var orderedClassifications = SalesClassificationReport.OrderByDescending(c => c.TotalSales).ToList();
                SalesClassificationReport.Clear();
                foreach (var item in orderedClassifications)
                {
                    SalesClassificationReport.Add(item);
                }

                CategoryReportTotalSales = grandCategorySales;
                CategoryReportTotalQty = SalesClassificationReport.Sum(c => c.QuantitySold);
                CategoryReportActiveCount = SalesClassificationReport.Count;
                CategoryReportTopCategory = SalesClassificationReport.Count > 0 ? SalesClassificationReport[0].CategoryName : "—";

                // ==================== 3. مبيعات منتج موسع (EXTENDED PRODUCT SALES - TAB 2) ====================
                ProductSalesReport.Clear();
                SingleProductCycleReport.Clear();

                var selectedProdId = SelectedFilterProduct?.Id ?? 0;

                if (selectedProdId > 0)
                {
                    // ===== SINGLE PRODUCT DETAILED CYCLE MODE =====
                    IsSingleProductMode = true;
                    var prodEntity = allProducts.FirstOrDefault(p => p.Id == selectedProdId);
                    SingleProductName = prodEntity != null ? (string.IsNullOrEmpty(prodEntity.NameAR) ? prodEntity.NameEN : prodEntity.NameAR) : SelectedFilterProduct?.Name ?? "صنف";
                    SingleProductSku = prodEntity?.SKU ?? "—";
                    SingleProductPrice = prodEntity?.SellingPrice ?? 0;
                    SingleProductCurrentStock = prodEntity != null ? (int)prodEntity.CurrentStock : 0;
                    SingleProductCategory = prodEntity != null && catMap.TryGetValue(prodEntity.CategoryId, out var cn) ? cn : "عام";

                    var prodLineItems = filteredLineItems.Where(li => li.ProductId == selectedProdId).ToList();
                    foreach (var li in prodLineItems)
                    {
                        var sale = saleMap.TryGetValue(li.SaleId, out var s) ? s : null;
                        var cashier = sale != null && userMap.TryGetValue(sale.CashierId, out var cn2) ? cn2 : "كاشير";
                        var recNumber = sale != null ? (!string.IsNullOrEmpty(sale.ReceiptNumber) ? sale.ReceiptNumber : $"#{sale.Id}") : $"#{li.SaleId}";
                        var saleDate = sale?.Date ?? DateTime.Today;

                        SingleProductCycleReport.Add(new ProductCycleTransactionItem
                        {
                            ReceiptNumber = recNumber,
                            Date = saleDate,
                            CashierName = cashier,
                            Quantity = (int)li.Quantity,
                            UnitPrice = li.UnitPrice,
                            DiscountAmount = sale?.DiscountAmount ?? 0,
                            LineTotal = li.LineTotal
                        });
                    }

                    var orderedCycle = SingleProductCycleReport.OrderByDescending(x => x.Date).ToList();
                    SingleProductCycleReport.Clear();
                    foreach (var item in orderedCycle)
                    {
                        SingleProductCycleReport.Add(item);
                    }

                    ProductReportTotalSales = SingleProductCycleReport.Sum(x => x.LineTotal);
                    ProductReportTotalQty = SingleProductCycleReport.Sum(x => x.Quantity);
                    ProductReportActiveCount = SingleProductCycleReport.Count;
                    ProductReportTopProduct = SingleProductName;
                }
                else
                {
                    // ===== ALL PRODUCTS AGGREGATE SUMMARY MODE =====
                    IsSingleProductMode = false;
                    var grandProductSales = filteredLineItems.Sum(li => li.LineTotal);

                    var prodGroups = filteredLineItems.GroupBy(li => li.ProductId).ToList();
                    foreach (var g in prodGroups)
                    {
                        var p = productMap.TryGetValue(g.Key, out var pp) ? pp : null;
                        var name = p != null ? (string.IsNullOrEmpty(p.NameAR) ? p.NameEN : p.NameAR) : $"صنف #{g.Key}";
                        var sku = p?.SKU ?? string.Empty;
                        var barcode = p?.Barcode ?? string.Empty;
                        var catId = p?.CategoryId ?? 0;
                        var catName = catMap.TryGetValue(catId, out var cn) ? cn : "عام";
                        
                        var qty = (int)g.Sum(li => li.Quantity);
                        var tot = g.Sum(li => li.LineTotal);
                        var unitPrice = qty > 0 ? Math.Round(tot / qty, 2) : (p?.SellingPrice ?? 0);
                        var pct = grandProductSales > 0 ? (double)(tot / grandProductSales * 100) : 0;

                        ProductSalesReport.Add(new ProductSalesReportItem
                        {
                            ProductId = g.Key,
                            ProductName = name,
                            SKU = sku,
                            Barcode = barcode,
                            CategoryName = catName,
                            UnitPrice = unitPrice,
                            QuantitySold = qty,
                            TotalSales = tot,
                            Percentage = pct
                        });
                    }

                    var orderedProducts = ProductSalesReport.OrderByDescending(p => p.TotalSales).ToList();
                    ProductSalesReport.Clear();
                    foreach (var item in orderedProducts)
                    {
                        ProductSalesReport.Add(item);
                    }

                    ProductReportTotalSales = grandProductSales;
                    ProductReportTotalQty = ProductSalesReport.Sum(p => p.QuantitySold);
                    ProductReportActiveCount = ProductSalesReport.Count;
                    ProductReportTopProduct = ProductSalesReport.Count > 0 ? ProductSalesReport[0].ProductName : "—";
                }

                // ==================== 4. سجل الفواتير (INVOICES - TAB 3) ====================
                SalesInvoicesReport.Clear();
                var lineItemsGroupedBySale = allLineItems.GroupBy(li => li.SaleId).ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = (int)g.Sum(li => li.Quantity),
                        Summary = string.Join("، ", g.Select(li =>
                        {
                            var pName = productMap.TryGetValue(li.ProductId, out var p)
                                ? (string.IsNullOrEmpty(p.NameAR) ? p.NameEN : p.NameAR)
                                : $"منتج #{li.ProductId}";
                            return $"{pName} (x{(int)li.Quantity})";
                        }))
                    });

                foreach (var s in filteredSales.OrderByDescending(x => x.Date))
                {
                    var hasData = lineItemsGroupedBySale.TryGetValue(s.Id, out var itemData);
                    var count = hasData ? itemData!.Count : 1;
                    var summary = hasData ? itemData!.Summary : "—";

                    SalesInvoicesReport.Add(new SalesClosingReportItem
                    {
                        SaleId = s.Id,
                        ReceiptNumber = string.IsNullOrEmpty(s.ReceiptNumber) ? $"#{s.Id}" : s.ReceiptNumber,
                        Date = s.Date,
                        CashierName = userMap.TryGetValue(s.CashierId, out var n) ? n : "كاشير عام",
                        ItemsCount = count,
                        ItemsSummary = string.IsNullOrEmpty(summary) ? "—" : summary,
                        SubTotal = s.SubTotal,
                        DiscountAmount = s.DiscountAmount,
                        TaxAmount = s.TaxAmount,
                        Total = s.Total,
                        PaymentMethod = "نقدي (Cash)",
                        Status = string.IsNullOrEmpty(s.Status) ? "مكتمل" : s.Status
                    });
                }

                // ==================== General Financial Totals ====================
                TotalRevenue = filteredSales.Sum(s => s.Total);
                TotalExpenses = filteredExpenses.Sum(e => e.Amount);
                TotalReturns = filteredReturns.Sum(r => r.TotalRefundAmount);
                TotalDiscounts = filteredSales.Sum(s => s.DiscountAmount);
                TotalTax = filteredSales.Sum(s => s.TaxAmount);
                NetProfit = TotalRevenue - TotalReturns - TotalExpenses;

                HasReportData = ClosedRegisterReport.Count > 0 || SalesClassificationReport.Count > 0 || ProductSalesReport.Count > 0 || SingleProductCycleReport.Count > 0 || SalesInvoicesReport.Count > 0;
                CurrentPage = 1;
                TotalPages = 1;

                ReportStatus = HasReportData
                    ? $"تم استخراج بيانات التقرير بنجاح للفترة ({StartDate:yyyy/MM/dd} — {EndDate:yyyy/MM/dd})"
                    : $"لا توجد بيانات مسجلة في النظام خلال الفترة المحددة ({StartDate:yyyy/MM/dd} — {EndDate:yyyy/MM/dd})";
            }
            catch (Exception ex)
            {
                ReportStatus = $"خطأ أثناء استخراج التقرير: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== PRINT & EXPORT ====================

        [RelayCommand]
        private void PrintReport(FrameworkElement? visualElement)
        {
            if (!HasReportData && ClosedRegisterReport.Count == 0 && SalesClassificationReport.Count == 0 && ProductSalesReport.Count == 0 && SingleProductCycleReport.Count == 0 && SalesInvoicesReport.Count == 0)
            {
                MessageBox.Show("يرجى إنشاء التقرير أولاً قبل الطباعة!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    if (visualElement != null)
                    {
                        var reportName = SelectedReportTab switch
                        {
                            1 => "Axon_Category_Sales_Report",
                            2 => IsSingleProductMode ? $"Axon_Product_Cycle_{SingleProductSku}" : "Axon_Extended_Product_Sales_Report",
                            3 => "Axon_Invoices_Report",
                            _ => "Axon_Closed_Box_Report"
                        };
                        printDialog.PrintVisual(visualElement, $"{reportName}_{DateTime.Now:yyyyMMdd_HHmm}");
                    }
                    else
                    {
                        MessageBox.Show("تم إرسال التقرير إلى الطابعة بنجاح.", "تمت الطباعة", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر إتمام عملية الطباعة: {ex.Message}", "خطأ في الطباعة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ExportExcelAsync(FrameworkElement? visualElement)
        {
            if (!HasReportData)
            {
                MessageBox.Show("لا توجد بيانات. اضغط 'إنشاء التقرير' أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var reportTypeStr = SelectedReportTab switch
                {
                    1 => "Category_Sales",
                    2 => IsSingleProductMode ? $"Product_Cycle_{SingleProductSku}" : "Extended_Product_Sales",
                    3 => "Invoices_Ledger",
                    _ => "Closed_Box"
                };

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "مستند PDF Document (*.pdf)|*.pdf|" +
                             "ملف إكسيل Excel (*.xlsx)|*.xlsx|" +
                             "مستند وورد Word Document (*.docx)|*.docx|" +
                             "ملف نصوص مفصولة CSV (*.csv)|*.csv|" +
                             "تقرير ويب HTML (*.html)|*.html|" +
                             "ملف نصي Text (*.txt)|*.txt|" +
                             "كافة الملفات (*.*)|*.*",
                    DefaultExt = ".pdf",
                    FileName = $"VelouraPOS_{reportTypeStr}_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.pdf",
                    Title = "حدد مكان وامتداد حفظ التقرير على الجهاز"
                };
                if (dialog.ShowDialog() != true) return;

                var filePath = dialog.FileName;
                var ext = Path.GetExtension(filePath).ToLowerInvariant();

                if (ext == ".pdf" && visualElement != null)
                {
                    MemoryStream? imgStream = null;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var targetElement = visualElement as FrameworkElement ?? visualElement;
                            double w = targetElement.ActualWidth > 0 ? targetElement.ActualWidth : 840;
                            double h = targetElement.ActualHeight > 0 ? targetElement.ActualHeight : 1000;
                            if (w < 800) w = 840;

                            double scale = 300.0 / 96.0;
                            int pxW = (int)(w * scale);
                            int pxH = (int)(h * scale);

                            var vb = new VisualBrush(targetElement)
                            {
                                Stretch = Stretch.None,
                                AlignmentX = AlignmentX.Left,
                                AlignmentY = AlignmentY.Top
                            };

                            var dv1 = new DrawingVisual();
                            using (var dc1 = dv1.RenderOpen())
                            {
                                dc1.DrawRectangle(vb, null, new Rect(0, 0, w, h));
                            }

                            var rtb1 = new System.Windows.Media.Imaging.RenderTargetBitmap(pxW, pxH, 300, 300, System.Windows.Media.PixelFormats.Pbgra32);
                            rtb1.Render(dv1);

                            // Un-mirror WPF RTL RenderTargetBitmap flip horizontally
                            var dv2 = new DrawingVisual();
                            using (var dc2 = dv2.RenderOpen())
                            {
                                dc2.PushTransform(new ScaleTransform(-1, 1, pxW / 2.0, 0));
                                dc2.DrawImage(rtb1, new Rect(0, 0, pxW, pxH));
                            }

                            var rtbFlipped = new System.Windows.Media.Imaging.RenderTargetBitmap(pxW, pxH, 300, 300, System.Windows.Media.PixelFormats.Pbgra32);
                            rtbFlipped.Render(dv2);

                            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFlipped));

                            imgStream = new MemoryStream();
                            encoder.Save(imgStream);
                            imgStream.Position = 0;
                        }
                        catch { }
                    });

                    if (imgStream != null)
                    {
                        await Task.Run(() =>
                        {
                            if (PdfSharp.Fonts.GlobalFontSettings.FontResolver == null)
                            {
                                try { PdfSharp.Fonts.GlobalFontSettings.FontResolver = new AxonPdfFontResolver(); } catch { }
                            }

                            using var pdf = new PdfSharp.Pdf.PdfDocument();
                            pdf.Info.Title = "Veloura POS Financial Report";
                            var page = pdf.AddPage();
                            page.Size = PdfSharp.PageSize.A4;

                            using var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
                            using var ximg = PdfSharp.Drawing.XImage.FromStream(imgStream);

                            double margin = 15;
                            double targetW = page.Width.Point - (margin * 2);
                            double targetH = (ximg.PixelHeight / (double)ximg.PixelWidth) * targetW;

                            if (targetH > page.Height.Point - (margin * 2))
                            {
                                targetH = page.Height.Point - (margin * 2);
                                targetW = (ximg.PixelWidth / (double)ximg.PixelHeight) * targetH;
                            }

                            double posX = (page.Width.Point - targetW) / 2;
                            double posY = margin;

                            gfx.DrawImage(ximg, posX, posY, targetW, targetH);
                            pdf.Save(filePath);
                        });

                        MessageBox.Show("تم تصدير التقرير كملف PDF بنفس الشكل المطابق تماماً للبرنامج بنجاح!", "تم التصدير بنجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                await Task.Run(() =>
                {
                    using var wb = new XLWorkbook();
                    wb.Style.Font.FontName = "Arial";

                    if (SelectedReportTab == 1)
                    {
                        // Tab 1: Category Sales Sheet
                        var ws = wb.Worksheets.Add("تصنيف المبيعات");
                        ws.RightToLeft = true;

                        ws.Cell(1, 1).Value = "تقرير تصنيف مبيعات الأقسام — Axon POS";
                        ws.Cell(1, 1).Style.Font.Bold = true;
                        ws.Cell(1, 1).Style.Font.FontSize = 16;
                        ws.Range(1, 1, 1, 6).Merge();

                        ws.Cell(2, 1).Value = $"الفترة: من {StartDate:yyyy/MM/dd} إلى {EndDate:yyyy/MM/dd} | القسم المختار: {SelectedCategoryTitle}";
                        ws.Range(2, 1, 2, 6).Merge();

                        int r = 4;
                        var hdrs = new[] { "كود القسم", "اسم القسم / التصنيف", "عدد الأصناف", "الكمية المباعة", "إجمالي المبيعات (ج.م)", "النسبة %" };
                        for (int c = 0; c < hdrs.Length; c++)
                        {
                            ws.Cell(r, c + 1).Value = hdrs[c];
                            ws.Cell(r, c + 1).Style.Font.Bold = true;
                            ws.Cell(r, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#D90429");
                            ws.Cell(r, c + 1).Style.Font.FontColor = XLColor.White;
                        }

                        r++;
                        foreach (var i in SalesClassificationReport)
                        {
                            ws.Cell(r, 1).Value = i.CategoryCode;
                            ws.Cell(r, 2).Value = i.CategoryName;
                            ws.Cell(r, 3).Value = i.DistinctProductsCount;
                            ws.Cell(r, 4).Value = i.QuantitySold;
                            ws.Cell(r, 5).Value = i.TotalSales;
                            ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 6).Value = i.PercentageDisplay;
                            r++;
                        }

                        ws.Cell(r, 2).Value = "الإجمالي العام:";
                        ws.Cell(r, 2).Style.Font.Bold = true;
                        ws.Cell(r, 4).Value = CategoryReportTotalQty;
                        ws.Cell(r, 4).Style.Font.Bold = true;
                        ws.Cell(r, 5).Value = CategoryReportTotalSales;
                        ws.Cell(r, 5).Style.Font.Bold = true;
                        ws.Cell(r, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD700");
                        ws.Cell(r, 6).Value = "100%";
                        ws.Cell(r, 6).Style.Font.Bold = true;

                        ws.Columns().AdjustToContents();
                    }
                    else if (SelectedReportTab == 2)
                    {
                        if (IsSingleProductMode)
                        {
                            // Tab 2 (Single Product Cycle Mode): Individual Product Transaction Sheet
                            var ws = wb.Worksheets.Add("دورة مبيعات المنتج");
                            ws.RightToLeft = true;

                            ws.Cell(1, 1).Value = $"تقرير دورة مبيعات المنتج: {SingleProductName} ({SingleProductSku}) — Axon POS";
                            ws.Cell(1, 1).Style.Font.Bold = true;
                            ws.Cell(1, 1).Style.Font.FontSize = 16;
                            ws.Range(1, 1, 1, 7).Merge();

                            ws.Cell(2, 1).Value = $"الفترة: من {StartDate:yyyy/MM/dd} إلى {EndDate:yyyy/MM/dd} | القسم: {SingleProductCategory} | السعر الحالي: {SingleProductPrice:N2} ج.م | المخزون الحالي: {SingleProductCurrentStock} قطعة";
                            ws.Range(2, 1, 2, 7).Merge();

                            int r = 4;
                            var hdrs = new[] { "رقم الفاتورة", "التاريخ والوقت", "الكاشير", "الكمية المباعة", "سعر الوحدة (ج.م)", "الخصم (ج.م)", "إجمالي البيع (ج.م)" };
                            for (int c = 0; c < hdrs.Length; c++)
                            {
                                ws.Cell(r, c + 1).Value = hdrs[c];
                                ws.Cell(r, c + 1).Style.Font.Bold = true;
                                ws.Cell(r, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#D90429");
                                ws.Cell(r, c + 1).Style.Font.FontColor = XLColor.White;
                            }

                            r++;
                            foreach (var i in SingleProductCycleReport)
                            {
                                ws.Cell(r, 1).Value = i.ReceiptNumber;
                                ws.Cell(r, 2).Value = i.DateDisplay;
                                ws.Cell(r, 3).Value = i.CashierName;
                                ws.Cell(r, 4).Value = i.Quantity;
                                ws.Cell(r, 5).Value = i.UnitPrice;
                                ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0.00";
                                ws.Cell(r, 6).Value = i.DiscountAmount;
                                ws.Cell(r, 6).Style.NumberFormat.Format = "#,##0.00";
                                ws.Cell(r, 7).Value = i.LineTotal;
                                ws.Cell(r, 7).Style.NumberFormat.Format = "#,##0.00";
                                r++;
                            }

                            ws.Cell(r, 3).Value = "الإجمالي الشامل:";
                            ws.Cell(r, 3).Style.Font.Bold = true;
                            ws.Cell(r, 4).Value = ProductReportTotalQty;
                            ws.Cell(r, 4).Style.Font.Bold = true;
                            ws.Cell(r, 7).Value = ProductReportTotalSales;
                            ws.Cell(r, 7).Style.Font.Bold = true;
                            ws.Cell(r, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD700");

                            ws.Columns().AdjustToContents();
                        }
                        else
                        {
                            // Tab 2 (All Products Mode): Extended Product Sales Sheet
                            var ws = wb.Worksheets.Add("مبيعات منتج موسع");
                            ws.RightToLeft = true;

                            ws.Cell(1, 1).Value = "تقرير مبيعات منتج موسع — Axon POS";
                            ws.Cell(1, 1).Style.Font.Bold = true;
                            ws.Cell(1, 1).Style.Font.FontSize = 16;
                            ws.Range(1, 1, 1, 7).Merge();

                            ws.Cell(2, 1).Value = $"الفترة: من {StartDate:yyyy/MM/dd} إلى {EndDate:yyyy/MM/dd} | القسم: {SelectedCategoryTitle}";
                            ws.Range(2, 1, 2, 7).Merge();

                            int r = 4;
                            var hdrs = new[] { "كود الصنف (SKU)", "اسم المنتج", "القسم / التصنيف", "سعر الوحدة (ج.م)", "الكمية المباعة", "إجمالي المبيعات (ج.م)", "النسبة %" };
                            for (int c = 0; c < hdrs.Length; c++)
                            {
                                ws.Cell(r, c + 1).Value = hdrs[c];
                                ws.Cell(r, c + 1).Style.Font.Bold = true;
                                ws.Cell(r, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#D90429");
                                ws.Cell(r, c + 1).Style.Font.FontColor = XLColor.White;
                            }

                            r++;
                            foreach (var i in ProductSalesReport)
                            {
                                ws.Cell(r, 1).Value = i.DisplayCode;
                                ws.Cell(r, 2).Value = i.ProductName;
                                ws.Cell(r, 3).Value = i.CategoryName;
                                ws.Cell(r, 4).Value = i.UnitPrice;
                                ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                                ws.Cell(r, 5).Value = i.QuantitySold;
                                ws.Cell(r, 6).Value = i.TotalSales;
                                ws.Cell(r, 6).Style.NumberFormat.Format = "#,##0.00";
                                ws.Cell(r, 7).Value = i.PercentageDisplay;
                                r++;
                            }

                            ws.Cell(r, 3).Value = "الإجمالي الكلي:";
                            ws.Cell(r, 3).Style.Font.Bold = true;
                            ws.Cell(r, 5).Value = ProductReportTotalQty;
                            ws.Cell(r, 5).Style.Font.Bold = true;
                            ws.Cell(r, 6).Value = ProductReportTotalSales;
                            ws.Cell(r, 6).Style.Font.Bold = true;
                            ws.Cell(r, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD700");
                            ws.Cell(r, 7).Value = "100%";
                            ws.Cell(r, 7).Style.Font.Bold = true;

                            ws.Columns().AdjustToContents();
                        }
                    }
                    else if (SelectedReportTab == 3)
                    {
                        // Tab 3: Invoices Ledger Sheet
                        var ws = wb.Worksheets.Add("سجل الفواتير والمبيعات");
                        ws.RightToLeft = true;

                        ws.Cell(1, 1).Value = "تقرير سجل الفواتير والمبيعات التفصيلي — Axon POS";
                        ws.Cell(1, 1).Style.Font.Bold = true;
                        ws.Cell(1, 1).Style.Font.FontSize = 16;
                        ws.Range(1, 1, 1, 9).Merge();

                        ws.Cell(2, 1).Value = $"الفترة: من {StartDate:yyyy/MM/dd} إلى {EndDate:yyyy/MM/dd} | إجمالي الفواتير: {SalesInvoicesReport.Count}";
                        ws.Range(2, 1, 2, 9).Merge();

                        int r = 4;
                        var hdrs = new[] { "رقم الفاتورة", "التاريخ والوقت", "الكاشير", "عدد القطع", "المجموع الفرعي", "الخصم", "الضريبة", "الإجمالي النهائي (ج.م)", "الحالة" };
                        for (int c = 0; c < hdrs.Length; c++)
                        {
                            ws.Cell(r, c + 1).Value = hdrs[c];
                            ws.Cell(r, c + 1).Style.Font.Bold = true;
                            ws.Cell(r, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#D90429");
                            ws.Cell(r, c + 1).Style.Font.FontColor = XLColor.White;
                        }

                        r++;
                        foreach (var i in SalesInvoicesReport)
                        {
                            ws.Cell(r, 1).Value = i.ReceiptNumber;
                            ws.Cell(r, 2).Value = i.DateDisplay;
                            ws.Cell(r, 3).Value = i.CashierName;
                            ws.Cell(r, 4).Value = i.ItemsCount;
                            ws.Cell(r, 5).Value = i.SubTotal;
                            ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 6).Value = i.DiscountAmount;
                            ws.Cell(r, 6).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 7).Value = i.TaxAmount;
                            ws.Cell(r, 7).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 8).Value = i.Total;
                            ws.Cell(r, 8).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 8).Style.Font.Bold = true;
                            ws.Cell(r, 9).Value = i.Status;
                            r++;
                        }

                        ws.Cell(r, 3).Value = "الإجمالي العام:";
                        ws.Cell(r, 3).Style.Font.Bold = true;
                        ws.Cell(r, 4).Value = SalesInvoicesReport.Sum(x => x.ItemsCount);
                        ws.Cell(r, 4).Style.Font.Bold = true;
                        ws.Cell(r, 5).Value = SalesInvoicesReport.Sum(x => x.SubTotal);
                        ws.Cell(r, 5).Style.Font.Bold = true;
                        ws.Cell(r, 6).Value = TotalDiscounts;
                        ws.Cell(r, 6).Style.Font.Bold = true;
                        ws.Cell(r, 7).Value = TotalTax;
                        ws.Cell(r, 7).Style.Font.Bold = true;
                        ws.Cell(r, 8).Value = TotalRevenue;
                        ws.Cell(r, 8).Style.Font.Bold = true;
                        ws.Cell(r, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD700");

                        ws.Columns().AdjustToContents();
                    }
                    else
                    {
                        // Default / Tab 0: Closed Box Sheet
                        var ws = wb.Worksheets.Add("الصندوق المغلق");
                        ws.RightToLeft = true;

                        ws.Cell(1, 1).Value = "VELOURA — تقرير تقفيل الشيفت والصندوق المغلق التفصيلي";
                        ws.Cell(1, 1).Style.Font.Bold = true;
                        ws.Cell(1, 1).Style.Font.FontSize = 16;
                        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
                        ws.Range(1, 1, 1, 8).Merge();

                        ws.Cell(2, 1).Value = $"الفترة: من {StartDate:yyyy/MM/dd} إلى {EndDate:yyyy/MM/dd} | إجمالي الفواتير: {ClosedBoxInvoicesCount} | صافي الدرج: {ClosedBoxNetTotal:N2} ج.م";
                        ws.Range(2, 1, 2, 8).Merge();

                        // Table 1: Shift Closures Summary Table
                        int r = 4;
                        ws.Cell(r, 1).Value = "أولاً: ملخص تقفيلات الوردية والشيفتات المسجلة:";
                        ws.Cell(r, 1).Style.Font.Bold = true;
                        ws.Cell(r, 1).Style.Font.FontSize = 12;
                        ws.Range(r, 1, r, 8).Merge();

                        r++;
                        var hdrs1 = new[] { "رمز التقفيلة", "الكاشير المسؤول", "توقيت بداية ونهاية الوردية", "عدد الفواتير", "مبيعات الكاش (ج.م)", "المرتجعات (ج.م)", "صافي التقفيلة (ج.م)", "الحالة" };
                        for (int c = 0; c < hdrs1.Length; c++)
                        {
                            ws.Cell(r, c + 1).Value = hdrs1[c];
                            ws.Cell(r, c + 1).Style.Font.Bold = true;
                            ws.Cell(r, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                            ws.Cell(r, c + 1).Style.Font.FontColor = XLColor.White;
                        }

                        r++;
                        foreach (var i in ClosedRegisterReport)
                        {
                            ws.Cell(r, 1).Value = i.ClosureCode;
                            ws.Cell(r, 2).Value = i.CashierName;
                            ws.Cell(r, 3).Value = i.PeriodDisplay;
                            ws.Cell(r, 4).Value = i.InvoicesCount;
                            ws.Cell(r, 5).Value = i.CashSales;
                            ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 6).Value = i.ReturnsAmount;
                            ws.Cell(r, 6).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 7).Value = i.NetSales;
                            ws.Cell(r, 7).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 7).Style.Font.Bold = true;
                            ws.Cell(r, 8).Value = i.Status;
                            r++;
                        }

                        ws.Cell(r, 3).Value = "الإجمالي العام:";
                        ws.Cell(r, 3).Style.Font.Bold = true;
                        ws.Cell(r, 4).Value = ClosedBoxInvoicesCount;
                        ws.Cell(r, 4).Style.Font.Bold = true;
                        ws.Cell(r, 5).Value = ClosedBoxCashTotal;
                        ws.Cell(r, 5).Style.Font.Bold = true;
                        ws.Cell(r, 6).Value = ClosedBoxReturnTotal;
                        ws.Cell(r, 6).Style.Font.Bold = true;
                        ws.Cell(r, 7).Value = ClosedBoxNetTotal;
                        ws.Cell(r, 7).Style.Font.Bold = true;
                        ws.Cell(r, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD700");

                        // Table 2: Detailed Shift Invoices & Sold Items Breakdown
                        r += 3;
                        ws.Cell(r, 1).Value = "ثانياً: بيانات الفواتير والأصناف المباعة المندرجة تحت التقرير:";
                        ws.Cell(r, 1).Style.Font.Bold = true;
                        ws.Cell(r, 1).Style.Font.FontSize = 12;
                        ws.Range(r, 1, r, 7).Merge();

                        r++;
                        var hdrs2 = new[] { "رقم الفاتورة", "تاريخ ووقت الفاتورة", "الأصناف والقطع المباعة", "الكاشير", "العدد", "طريقة الدفع", "إجمالي الفاتورة (ج.م)" };
                        for (int c = 0; c < hdrs2.Length; c++)
                        {
                            ws.Cell(r, c + 1).Value = hdrs2[c];
                            ws.Cell(r, c + 1).Style.Font.Bold = true;
                            ws.Cell(r, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                            ws.Cell(r, c + 1).Style.Font.FontColor = XLColor.White;
                        }

                        r++;
                        foreach (var i in SalesInvoicesReport)
                        {
                            ws.Cell(r, 1).Value = i.ReceiptNumber;
                            ws.Cell(r, 2).Value = i.DateDisplay;
                            ws.Cell(r, 3).Value = i.ItemsSummary;
                            ws.Cell(r, 4).Value = i.CashierName;
                            ws.Cell(r, 5).Value = i.ItemsCount;
                            ws.Cell(r, 6).Value = i.PaymentMethod;
                            ws.Cell(r, 7).Value = i.Total;
                            ws.Cell(r, 7).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 7).Style.Font.Bold = true;
                            r++;
                        }

                        ws.Cell(r, 4).Value = "إجمالي الفواتير:";
                        ws.Cell(r, 4).Style.Font.Bold = true;
                        ws.Cell(r, 5).Value = SalesInvoicesReport.Sum(x => x.ItemsCount);
                        ws.Cell(r, 5).Style.Font.Bold = true;
                        ws.Cell(r, 7).Value = ClosedBoxCashTotal;
                        ws.Cell(r, 7).Style.Font.Bold = true;
                        ws.Cell(r, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD700");

                        ws.Columns().AdjustToContents();
                    }

                    if (ext == ".xlsx" || ext == ".xlsm" || ext == ".xltx" || ext == ".xltm")
                    {
                        wb.SaveAs(filePath);
                    }
                    else if (ext == ".csv" || ext == ".txt")
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"تقرير VELOURA — الفترة من {StartDate:yyyy/MM/dd} إلى {EndDate:yyyy/MM/dd}");
                        sb.AppendLine();

                        if (SelectedReportTab == 1)
                        {
                            sb.AppendLine("كود القسم,اسم القسم / التصنيف,عدد الأصناف,الكمية المباعة,إجمالي المبيعات (ج.م),النسبة %");
                            foreach (var i in SalesClassificationReport)
                                sb.AppendLine($"\"{i.CategoryCode}\",\"{i.CategoryName}\",{i.DistinctProductsCount},{i.QuantitySold},{i.TotalSales},{i.PercentageDisplay}");
                            sb.AppendLine($",الإجمالي العام,,{CategoryReportTotalQty},{CategoryReportTotalSales},100%");
                        }
                        else if (SelectedReportTab == 2)
                        {
                            if (IsSingleProductMode)
                            {
                                sb.AppendLine("رقم الفاتورة,التاريخ والوقت,الكاشير,الكمية المباعة,سعر الوحدة,الخصم,إجمالي البيع");
                                foreach (var i in SingleProductCycleReport)
                                    sb.AppendLine($"\"{i.ReceiptNumber}\",\"{i.DateDisplay}\",\"{i.CashierName}\",{i.Quantity},{i.UnitPrice},{i.DiscountAmount},{i.LineTotal}");
                                sb.AppendLine($",,الإجمالي الشامل,{ProductReportTotalQty},,,{ProductReportTotalSales}");
                            }
                            else
                            {
                                sb.AppendLine("كود الصنف,اسم المنتج,القسم / التصنيف,سعر الوحدة,الكمية المباعة,إجمالي المبيعات,النسبة %");
                                foreach (var i in ProductSalesReport)
                                    sb.AppendLine($"\"{i.DisplayCode}\",\"{i.ProductName}\",\"{i.CategoryName}\",{i.UnitPrice},{i.QuantitySold},{i.TotalSales},{i.PercentageDisplay}");
                                sb.AppendLine($",,,الإجمالي الكلي,{ProductReportTotalQty},{ProductReportTotalSales},100%");
                            }
                        }
                        else if (SelectedReportTab == 3)
                        {
                            sb.AppendLine("رقم الفاتورة,التاريخ والوقت,الكاشير,عدد القطع,المجموع الفرعي,الخصم,الضريبة,الإجمالي النهائي,الحالة");
                            foreach (var i in SalesInvoicesReport)
                                sb.AppendLine($"\"{i.ReceiptNumber}\",\"{i.DateDisplay}\",\"{i.CashierName}\",{i.ItemsCount},{i.SubTotal},{i.DiscountAmount},{i.TaxAmount},{i.Total},\"{i.Status}\"");
                            sb.AppendLine($",,,{SalesInvoicesReport.Sum(x=>x.ItemsCount)},{SalesInvoicesReport.Sum(x=>x.SubTotal)},{TotalDiscounts},{TotalTax},{TotalRevenue},");
                        }
                        else
                        {
                            sb.AppendLine("رقم التقفيلة,الكاشير المسؤول,توقيت بداية ونهاية الوردية,مبيعات الكاش (ج.م),المرتجعات (ج.م),صافي التقفيلة (ج.م),الحالة");
                            foreach (var i in ClosedRegisterReport)
                                sb.AppendLine($"\"{i.ClosureCode}\",\"{i.CashierName}\",\"{i.PeriodDisplay}\",{i.CashSales},{i.ReturnsAmount},{i.NetSales},\"{i.Status}\"");
                            sb.AppendLine();
                            sb.AppendLine("رقم الفاتورة,التاريخ والوقت,الأصناف والقطع المباعة,الكاشير,عدد القطع,طريقة الدفع,إجمالي الفاتورة (ج.م)");
                            foreach (var i in SalesInvoicesReport)
                                sb.AppendLine($"\"{i.ReceiptNumber}\",\"{i.DateDisplay}\",\"{i.ItemsSummary}\",\"{i.CashierName}\",{i.ItemsCount},\"{i.PaymentMethod}\",{i.Total}");
                        }

                        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                    }
                    else if (ext == ".pdf")
                    {
                        if (PdfSharp.Fonts.GlobalFontSettings.FontResolver == null)
                        {
                            try { PdfSharp.Fonts.GlobalFontSettings.FontResolver = new AxonPdfFontResolver(); } catch { }
                        }

                        using var pdf = new PdfSharp.Pdf.PdfDocument();
                        pdf.Info.Title = "Axon POS Report";
                        var page = pdf.AddPage();
                        page.Size = PdfSharp.PageSize.A4;

                        using var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
                        var fontTitle = new PdfSharp.Drawing.XFont("Arial", 16, PdfSharp.Drawing.XFontStyleEx.Bold);
                        var fontSub = new PdfSharp.Drawing.XFont("Arial", 10, PdfSharp.Drawing.XFontStyleEx.Regular);
                        var fontHeader = new PdfSharp.Drawing.XFont("Arial", 11, PdfSharp.Drawing.XFontStyleEx.Bold);
                        var fontBody = new PdfSharp.Drawing.XFont("Arial", 9, PdfSharp.Drawing.XFontStyleEx.Regular);

                        gfx.DrawString("Axon POS — Financial Report", fontTitle, PdfSharp.Drawing.XBrushes.DarkRed, new PdfSharp.Drawing.XRect(0, 35, page.Width, 25), PdfSharp.Drawing.XStringFormats.TopCenter);
                        gfx.DrawString($"Period: {StartDate:yyyy/MM/dd} - {EndDate:yyyy/MM/dd}", fontSub, PdfSharp.Drawing.XBrushes.DarkGray, new PdfSharp.Drawing.XRect(0, 65, page.Width, 20), PdfSharp.Drawing.XStringFormats.TopCenter);

                        double y = 105;
                        if (SelectedReportTab == 1)
                        {
                            gfx.DrawString("Category Sales Classification Report", fontHeader, PdfSharp.Drawing.XBrushes.Black, 40, y);
                            y += 25;
                            foreach (var i in SalesClassificationReport)
                            {
                                if (y > page.Height - 50) { page = pdf.AddPage(); y = 40; }
                                gfx.DrawString($"[{i.CategoryCode}] {i.CategoryName} | Qty: {i.QuantitySold} | Sales: {i.TotalSales:N2} LE ({i.PercentageDisplay})", fontBody, PdfSharp.Drawing.XBrushes.Black, 40, y);
                                y += 18;
                            }
                            gfx.DrawString($"Total Qty: {CategoryReportTotalQty} | Total Revenue: {CategoryReportTotalSales:N2} LE", fontHeader, PdfSharp.Drawing.XBrushes.DarkRed, 40, y + 10);
                        }
                        else if (SelectedReportTab == 2)
                        {
                            if (IsSingleProductMode)
                            {
                                gfx.DrawString($"Product Cycle Report: {SingleProductName} ({SingleProductSku})", fontHeader, PdfSharp.Drawing.XBrushes.Black, 40, y);
                                y += 25;
                                foreach (var i in SingleProductCycleReport)
                                {
                                    if (y > page.Height - 50) { page = pdf.AddPage(); y = 40; }
                                    gfx.DrawString($"Inv: #{i.ReceiptNumber} | Date: {i.DateDisplay} | Qty: {i.Quantity} | Total: {i.LineTotal:N2} LE", fontBody, PdfSharp.Drawing.XBrushes.Black, 40, y);
                                    y += 18;
                                }
                            }
                            else
                            {
                                gfx.DrawString("Extended Product Sales Report", fontHeader, PdfSharp.Drawing.XBrushes.Black, 40, y);
                                y += 25;
                                foreach (var i in ProductSalesReport)
                                {
                                    if (y > page.Height - 50) { page = pdf.AddPage(); y = 40; }
                                    gfx.DrawString($"[{i.DisplayCode}] {i.ProductName} ({i.CategoryName}) | Qty: {i.QuantitySold} | Sales: {i.TotalSales:N2} LE", fontBody, PdfSharp.Drawing.XBrushes.Black, 40, y);
                                    y += 18;
                                }
                            }
                        }
                        else if (SelectedReportTab == 3)
                        {
                            gfx.DrawString("Sales Invoices Ledger Report", fontHeader, PdfSharp.Drawing.XBrushes.Black, 40, y);
                            y += 25;
                            foreach (var i in SalesInvoicesReport)
                            {
                                if (y > page.Height - 50) { page = pdf.AddPage(); y = 40; }
                                gfx.DrawString($"Invoice #{i.ReceiptNumber} | Date: {i.DateDisplay} | Items: {i.ItemsCount} | Total: {i.Total:N2} LE", fontBody, PdfSharp.Drawing.XBrushes.Black, 40, y);
                                y += 18;
                            }
                            gfx.DrawString($"Total Revenue: {TotalRevenue:N2} LE", fontHeader, PdfSharp.Drawing.XBrushes.DarkRed, 40, y + 10);
                        }
                        else
                        {
                            gfx.DrawString("Closed Register Shift Report", fontHeader, PdfSharp.Drawing.XBrushes.Black, 40, y);
                            y += 25;
                            foreach (var i in ClosedRegisterReport)
                            {
                                if (y > page.Height - 50) { page = pdf.AddPage(); y = 40; }
                                gfx.DrawString($"{i.ClosureCode} | Cashier: {i.CashierName} | Cash: {i.CashSales:N2} LE | Returns: {i.ReturnsAmount:N2} LE | Net: {i.NetSales:N2} LE", fontBody, PdfSharp.Drawing.XBrushes.Black, 40, y);
                                y += 18;
                            }
                            gfx.DrawString($"Net Total: {ClosedBoxNetTotal:N2} LE", fontHeader, PdfSharp.Drawing.XBrushes.DarkRed, 40, y + 10);
                        }

                        pdf.Save(filePath);
                    }
                    else
                    {
                        // PDF, HTML, Word DOCX, or web formats
                        var sb = new StringBuilder();
                        sb.AppendLine("<!DOCTYPE html><html dir='rtl' lang='ar'><head><meta charset='utf-8'><title>تقرير VELOURA POS</title>");
                        sb.AppendLine("<style>");
                        sb.AppendLine("body{font-family:'Segoe UI',Tahoma,Arial,sans-serif;padding:30px;background:#fff;color:#0f172a;line-height:1.5;}");
                        sb.AppendLine(".brand{font-size:24px;font-weight:900;color:#1e3a8a;} .subtitle{color:#64748b;font-size:12px;margin-bottom:20px;}");
                        sb.AppendLine(".kpi-container{display:flex;gap:12px;margin-bottom:20px;} .kpi-card{flex:1;border:1px solid #e2e8f0;border-radius:8px;padding:12px;background:#f8fafc;}");
                        sb.AppendLine(".kpi-title{font-size:11px;font-weight:bold;color:#64748b;} .kpi-val{font-size:18px;font-weight:900;color:#0f172a;}");
                        sb.AppendLine("table{width:100%;border-collapse:collapse;margin-top:10px;margin-bottom:24px;} th,td{border:1px solid #cbd5e1;padding:8px 12px;text-align:right;}");
                        sb.AppendLine("th{background:#1e3a8a;color:#fff;font-weight:bold;font-size:12px;} tr:nth-child(even){background:#f8fafc;} .total-row{font-weight:bold;background:#fef3c7;color:#92400e;}");
                        sb.AppendLine(".sig-box{margin-top:40px;display:flex;justify-content:space-between;text-align:center;}");
                        sb.AppendLine("</style></head><body>");

                        sb.AppendLine("<div class='brand'>VELOURA • POS SYSTEM</div>");
                        sb.AppendLine($"<div class='subtitle'>تقرير مالي معتمد — الفترة من {StartDate:yyyy/MM/dd} إلى {EndDate:yyyy/MM/dd}</div><hr/>");

                        if (SelectedReportTab == 1)
                        {
                            sb.AppendLine("<h2>تقرير تصنيف مبيعات الأقسام</h2>");
                            sb.AppendLine("<table><thead><tr><th>كود القسم</th><th>اسم القسم / التصنيف</th><th>عدد الأصناف</th><th>الكمية المباعة</th><th>إجمالي المبيعات (ج.م)</th><th>النسبة %</th></tr></thead><tbody>");
                            foreach (var i in SalesClassificationReport)
                                sb.AppendLine($"<tr><td>{i.CategoryCode}</td><td>{i.CategoryName}</td><td>{i.DistinctProductsCount}</td><td>{i.QuantitySold}</td><td>{i.TotalSales:N2}</td><td>{i.PercentageDisplay}</td></tr>");
                            sb.AppendLine($"<tr class='total-row'><td colspan='3'>الإجمالي العام</td><td>{CategoryReportTotalQty}</td><td>{CategoryReportTotalSales:N2}</td><td>100%</td></tr></tbody></table>");
                        }
                        else if (SelectedReportTab == 2)
                        {
                            if (IsSingleProductMode)
                            {
                                sb.AppendLine($"<h2>تقرير دورة مبيعات المنتج: {SingleProductName} ({SingleProductSku})</h2>");
                                sb.AppendLine("<table><thead><tr><th>رقم الفاتورة</th><th>التاريخ والوقت</th><th>الكاشير</th><th>الكمية المباعة</th><th>سعر الوحدة</th><th>الخصم</th><th>إجمالي البيع</th></tr></thead><tbody>");
                                foreach (var i in SingleProductCycleReport)
                                    sb.AppendLine($"<tr><td>{i.ReceiptNumber}</td><td>{i.DateDisplay}</td><td>{i.CashierName}</td><td>{i.Quantity}</td><td>{i.UnitPrice:N2}</td><td>{i.DiscountAmount:N2}</td><td>{i.LineTotal:N2}</td></tr>");
                                sb.AppendLine($"<tr class='total-row'><td colspan='3'>الإجمالي الشامل</td><td>{ProductReportTotalQty}</td><td colspan='2'></td><td>{ProductReportTotalSales:N2}</td></tr></tbody></table>");
                            }
                            else
                            {
                                sb.AppendLine("<h2>تقرير مبيعات منتج موسع</h2>");
                                sb.AppendLine("<table><thead><tr><th>كود الصنف</th><th>اسم المنتج</th><th>القسم / التصنيف</th><th>سعر الوحدة</th><th>الكمية المباعة</th><th>إجمالي المبيعات</th><th>النسبة %</th></tr></thead><tbody>");
                                foreach (var i in ProductSalesReport)
                                    sb.AppendLine($"<tr><td>{i.DisplayCode}</td><td>{i.ProductName}</td><td>{i.CategoryName}</td><td>{i.UnitPrice:N2}</td><td>{i.QuantitySold}</td><td>{i.TotalSales:N2}</td><td>{i.PercentageDisplay}</td></tr>");
                                sb.AppendLine($"<tr class='total-row'><td colspan='4'>الإجمالي الكلي</td><td>{ProductReportTotalQty}</td><td>{ProductReportTotalSales:N2}</td><td>100%</td></tr></tbody></table>");
                            }
                        }
                        else if (SelectedReportTab == 3)
                        {
                            sb.AppendLine("<h2>تقرير سجل الفواتير والمبيعات التفصيلي</h2>");
                            sb.AppendLine("<table><thead><tr><th>رقم الفاتورة</th><th>التاريخ والوقت</th><th>الكاشير</th><th>عدد القطع</th><th>المجموع الفرعي</th><th>الخصم</th><th>الضريبة</th><th>الإجمالي النهائي</th><th>الحالة</th></tr></thead><tbody>");
                            foreach (var i in SalesInvoicesReport)
                                sb.AppendLine($"<tr><td>{i.ReceiptNumber}</td><td>{i.DateDisplay}</td><td>{i.CashierName}</td><td>{i.ItemsCount}</td><td>{i.SubTotal:N2}</td><td>{i.DiscountAmount:N2}</td><td>{i.TaxAmount:N2}</td><td>{i.Total:N2}</td><td>{i.Status}</td></tr>");
                            sb.AppendLine($"<tr class='total-row'><td colspan='3'>الإجمالي العام</td><td>{SalesInvoicesReport.Sum(x=>x.ItemsCount)}</td><td>{SalesInvoicesReport.Sum(x=>x.SubTotal):N2}</td><td>{TotalDiscounts:N2}</td><td>{TotalTax:N2}</td><td>{TotalRevenue:N2}</td><td></td></tr></tbody></table>");
                        }
                        else
                        {
                            sb.AppendLine("<div class='kpi-container'>");
                            sb.AppendLine($"<div class='kpi-card'><div class='kpi-title'>إجمالي مبيعات النقدي</div><div class='kpi-val'>{ClosedBoxCashTotal:N2} ج.م</div></div>");
                            sb.AppendLine($"<div class='kpi-card'><div class='kpi-title'>إجمالي المرتجعات</div><div class='kpi-val' style='color:#dc2626;'>{ClosedBoxReturnTotal:N2} ج.م</div></div>");
                            sb.AppendLine($"<div class='kpi-card'><div class='kpi-title'>عدد الفواتير الصادرة</div><div class='kpi-val' style='color:#2563eb;'>{ClosedBoxInvoicesCount} فاتورة</div></div>");
                            sb.AppendLine($"<div class='kpi-card'><div class='kpi-title'>صافي الدرج والصندوق</div><div class='kpi-val' style='color:#059669;'>{ClosedBoxNetTotal:N2} ج.م</div></div>");
                            sb.AppendLine("</div>");

                            sb.AppendLine("<h3>أولاً: ملخص تقفيلات الوردية والشيفتات المسجلة</h3>");
                            sb.AppendLine("<table><thead><tr><th>كود التقفيلة</th><th>الكاشير المسؤول</th><th>توقيت بداية ونهاية الوردية</th><th>الفواتير</th><th>مبيعات الكاش (ج.م)</th><th>المرتجعات (ج.م)</th><th>صافي التقفيلة (ج.م)</th><th>الحالة</th></tr></thead><tbody>");
                            foreach (var i in ClosedRegisterReport)
                                sb.AppendLine($"<tr><td>{i.ClosureCode}</td><td>{i.CashierName}</td><td>{i.PeriodDisplay}</td><td>{i.InvoicesCount}</td><td>{i.CashSales:N2}</td><td>{i.ReturnsAmount:N2}</td><td><b>{i.NetSales:N2}</b></td><td>{i.Status}</td></tr>");
                            sb.AppendLine($"<tr class='total-row'><td colspan='3'>الإجمالي العام</td><td>{ClosedBoxInvoicesCount}</td><td>{ClosedBoxCashTotal:N2}</td><td>{ClosedBoxReturnTotal:N2}</td><td>{ClosedBoxNetTotal:N2}</td><td></td></tr></tbody></table>");

                            sb.AppendLine("<h3>ثانياً: بيانات الفواتير والأصناف المباعة المندرجة تحت التقرير</h3>");
                            sb.AppendLine("<table><thead><tr><th>رقم الفاتورة</th><th>تاريخ ووقت الفاتورة</th><th>الأصناف والقطع المباعة</th><th>الكاشير</th><th>العدد</th><th>طريقة الدفع</th><th>إجمالي الفاتورة (ج.م)</th></tr></thead><tbody>");
                            foreach (var i in SalesInvoicesReport)
                                sb.AppendLine($"<tr><td><b>{i.ReceiptNumber}</b></td><td>{i.DateDisplay}</td><td><b>{i.ItemsSummary}</b></td><td>{i.CashierName}</td><td>{i.ItemsCount}</td><td>{i.PaymentMethod}</td><td><b>{i.Total:N2}</b></td></tr>");
                            sb.AppendLine($"<tr class='total-row'><td colspan='4'>إجمالي القطع والفواتير</td><td>{SalesInvoicesReport.Sum(x=>x.ItemsCount)}</td><td></td><td>{ClosedBoxCashTotal:N2}</td></tr></tbody></table>");

                            sb.AppendLine("<div class='sig-box'><div>توقيع الكاشير المسؤول:<br/><br/>.........................................</div><div>توقيع إدارة المحل:<br/><br/>.........................................</div></div>");
                        }

                        sb.AppendLine("</body></html>");
                        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                    }
                });

                Axon.UI.Views.AxonMessageBox.Show($"تم تصدير وحفظ التقرير بنجاح!\nالمسار: {filePath}", "نجاح التصدير", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل التصدير:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== ZOOM & NAVIGATION ====================

        [RelayCommand]
        private void ZoomIn()
        {
            if (ZoomScale < 2.0)
            {
                ZoomScale = Math.Round(ZoomScale + 0.15, 2);
                ZoomText = $"{(int)(ZoomScale * 100)}%";
            }
        }

        [RelayCommand]
        private void ZoomOut()
        {
            if (ZoomScale > 0.5)
            {
                ZoomScale = Math.Round(ZoomScale - 0.15, 2);
                ZoomText = $"{(int)(ZoomScale * 100)}%";
            }
        }

        [RelayCommand]
        private void ResetZoom()
        {
            ZoomScale = 1.0;
            ZoomText = "100%";
        }

        [RelayCommand] private void FirstPage() => CurrentPage = 1;
        [RelayCommand] private void PrevPage() { if (CurrentPage > 1) CurrentPage--; }
        [RelayCommand] private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }
        [RelayCommand] private void LastPage() => CurrentPage = TotalPages;
    }

    public class ProductFilterOptionModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int CategoryId { get; set; }
    }

    public class ProductCycleTransactionItem
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string DateDisplay => Date.ToString("yyyy/MM/dd  HH:mm");
        public string CashierName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class ClosedRegisterReportItem
    {
        public int SequenceNumber { get; set; }
        public string ClosureCode => $"تقفيلة #{SequenceNumber}";
        public string TerminalName { get; set; } = "Cash-PC";
        public string CashierName { get; set; } = "Admin";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string PeriodDisplay => $"{StartTime:yyyy/MM/dd HH:mm} — {EndTime:HH:mm}";
        public decimal CashSales { get; set; }
        public decimal ReturnsAmount { get; set; }
        public decimal GrossSales { get; set; }
        public decimal NetSales { get; set; }
        public int InvoicesCount { get; set; }
        public string Status { get; set; } = "مغلق";
    }

    public class SalesClassificationReportItem
    {
        public int CategoryId { get; set; }
        public string CategoryCode => $"CAT-{CategoryId:D3}";
        public string CategoryName { get; set; } = string.Empty;
        public int DistinctProductsCount { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalSales { get; set; }
        public double Percentage { get; set; }
        public string PercentageDisplay => $"{Percentage:F1}%";
        public decimal AveragePrice => QuantitySold > 0 ? Math.Round(TotalSales / QuantitySold, 2) : 0;
    }

    public class ProductSalesReportItem
    {
        public int ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string DisplayCode => !string.IsNullOrEmpty(SKU) ? SKU : (!string.IsNullOrEmpty(Barcode) ? Barcode : $"PRD-{ProductId:D4}");
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = "عام";
        public decimal UnitPrice { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalSales { get; set; }
        public double Percentage { get; set; }
        public string PercentageDisplay => $"{Percentage:F1}%";
    }

    public class SalesClosingReportItem
    {
        public int SaleId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string DateDisplay => Date.ToString("yyyy/MM/dd HH:mm");
        public string CashierName { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public string ItemsSummary { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = "نقدي (Cash)";
        public string Status { get; set; } = "مكتمل";
    }

    public class AxonPdfFontResolver : PdfSharp.Fonts.IFontResolver
    {
        public byte[]? GetFont(string faceName)
        {
            try
            {
                var fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
                var fontFile = faceName.ToLowerInvariant() switch
                {
                    "arial#b" => "arialbd.ttf",
                    "arial#i" => "ariali.ttf",
                    "arial#bi" => "arialbi.ttf",
                    _ => "arial.ttf"
                };

                var fullPath = System.IO.Path.Combine(fontsDir, fontFile);
                if (System.IO.File.Exists(fullPath))
                    return System.IO.File.ReadAllBytes(fullPath);

                var fallbackPath = System.IO.Path.Combine(fontsDir, "arial.ttf");
                if (System.IO.File.Exists(fallbackPath))
                    return System.IO.File.ReadAllBytes(fallbackPath);
            }
            catch { }
            return null;
        }

        public PdfSharp.Fonts.FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var suffix = "";
            if (isBold && isItalic) suffix = "#bi";
            else if (isBold) suffix = "#b";
            else if (isItalic) suffix = "#i";

            return new PdfSharp.Fonts.FontResolverInfo("Arial" + suffix);
        }
    }
}
