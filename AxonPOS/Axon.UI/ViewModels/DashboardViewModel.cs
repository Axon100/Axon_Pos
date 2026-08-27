using Axon.Application.Interfaces.Repositories;
using Axon.Domain.Entities;
using Axon.UI.Helpers;
using Axon.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Axon.UI.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        [ObservableProperty]
        private decimal _totalSales;
        
        [ObservableProperty]
        private decimal _totalProfit;
        
        [ObservableProperty]
        private decimal _totalExpenses;

        [ObservableProperty]
        private decimal _totalReturns;
        
        [ObservableProperty]
        private int _totalProducts;
        
        [ObservableProperty]
        private int _lowStockCount;
        
        [ObservableProperty]
        private int _totalTransactions;

        [ObservableProperty]
        private string _salesLinePathData = "M 30,140 L 570,140";

        [ObservableProperty]
        private string _salesAreaPathData = "M 30,145 L 30,140 L 570,140 L 570,145 Z";

        [ObservableProperty]
        private string _chartYAxisLabelMax = "0.00 ج.م";

        [ObservableProperty]
        private string _chartYAxisLabelMid = "0.00 ج.م";

        [ObservableProperty]
        private string _dateRange = string.Empty;

        [ObservableProperty]
        private string _selectedDateRange = "آخر 30 يوم";

        [ObservableProperty]
        private int _selectedYear = DateTime.Now.Year;

        [ObservableProperty]
        private DateTime _customStartDate = new DateTime(DateTime.Now.Year, 1, 1);

        [ObservableProperty]
        private DateTime _customEndDate = DateTime.Today;

        [ObservableProperty]
        private bool _isCustomDateVisible = false;

        [ObservableProperty]
        private bool _isYearSelectorVisible = false;

        public ObservableCollection<int> AvailableYears { get; } = new();

        public ObservableCollection<string> DateRanges { get; } = new()
        {
            "اليوم",
            "آخر 7 أيام",
            "آخر 30 يوم",
            "هذا الشهر",
            "سنوي",
            "فترة مخصصة"
        };

        public ObservableCollection<DailySalesBarItem> DailySalesChartData { get; } = new();
        public ObservableCollection<CategoryRevenueDistributionItem> CategoryDistributionData { get; } = new();
        public ObservableCollection<RecentSaleItem> RecentSales { get; } = new();
        public ObservableCollection<InventoryAlertItem> InventoryAlerts { get; } = new();

        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<SaleLineItem> _lineItemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<Return> _returnRepository;
        private readonly IRepository<Category> _categoryRepository;

        public DashboardViewModel(
            IRepository<Sale> saleRepository,
            IRepository<SaleLineItem> lineItemRepository,
            IRepository<Product> productRepository,
            IRepository<Expense> expenseRepository,
            IRepository<Return> returnRepository,
            IRepository<Category> categoryRepository)
        {
            _saleRepository = saleRepository;
            _lineItemRepository = lineItemRepository;
            _productRepository = productRepository;
            _expenseRepository = expenseRepository;
            _returnRepository = returnRepository;
            _categoryRepository = categoryRepository;

            Title = AppResources.GetString("Dashboard", "لوحة التحكم والقيادة");
            
            InitializeYears();
            _ = LoadDataAsync();
        }

        private void InitializeYears()
        {
            AvailableYears.Clear();
            int currentYear = DateTime.Now.Year;
            for (int y = currentYear + 1; y >= currentYear - 5; y--)
            {
                AvailableYears.Add(y);
            }
        }

        partial void OnSelectedDateRangeChanged(string value)
        {
            IsYearSelectorVisible = (value == "سنوي");
            IsCustomDateVisible = (value == "فترة مخصصة");
            _ = LoadDataAsync();
        }

        partial void OnSelectedYearChanged(int value)
        {
            if (IsYearSelectorVisible)
            {
                _ = LoadDataAsync();
            }
        }

        partial void OnCustomStartDateChanged(DateTime value)
        {
            if (IsCustomDateVisible)
            {
                _ = LoadDataAsync();
            }
        }

        partial void OnCustomEndDateChanged(DateTime value)
        {
            if (IsCustomDateVisible)
            {
                _ = LoadDataAsync();
            }
        }

        [RelayCommand]
        private void ViewAllSales()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (System.Windows.Application.Current.MainWindow?.DataContext is MainWindowViewModel mainVm)
                {
                    mainVm.NavigateCommand.Execute("Reports");
                }
            });
        }

        [RelayCommand]
        private void NavigateToInventory()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (System.Windows.Application.Current.MainWindow?.DataContext is MainWindowViewModel mainVm)
                {
                    mainVm.NavigateCommand.Execute("Inventory");
                }
            });
        }

        [RelayCommand]
        private void NavigateToPos()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (System.Windows.Application.Current.MainWindow?.DataContext is MainWindowViewModel mainVm)
                {
                    mainVm.NavigateCommand.Execute("PosTerminal");
                }
            });
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                DateTime startDate = DateTime.MinValue;
                DateTime endDate = DateTime.Now;
                var now = DateTime.Now;
                var arCulture = new CultureInfo("ar-EG");

                switch (SelectedDateRange)
                {
                    case "اليوم":
                        startDate = DateTime.Today;
                        endDate = DateTime.Today.AddDays(1).AddTicks(-1);
                        DateRange = $"{startDate.ToString("d MMMM yyyy", arCulture)}";
                        break;
                    case "آخر 7 أيام":
                        startDate = DateTime.Today.AddDays(-6);
                        endDate = DateTime.Now;
                        DateRange = $"{startDate.ToString("d MMMM", arCulture)} - {now.ToString("d MMMM yyyy", arCulture)}";
                        break;
                    case "آخر 30 يوم":
                        startDate = DateTime.Today.AddDays(-29);
                        endDate = DateTime.Now;
                        DateRange = $"{startDate.ToString("d MMMM", arCulture)} - {now.ToString("d MMMM yyyy", arCulture)}";
                        break;
                    case "هذا الشهر":
                        startDate = new DateTime(now.Year, now.Month, 1);
                        endDate = DateTime.Now;
                        DateRange = $"{startDate.ToString("d MMMM", arCulture)} - {now.ToString("d MMMM yyyy", arCulture)}";
                        break;
                    case "سنوي":
                        int targetYear = SelectedYear > 0 ? SelectedYear : now.Year;
                        startDate = new DateTime(targetYear, 1, 1);
                        endDate = new DateTime(targetYear, 12, 31, 23, 59, 59);
                        DateRange = $"التقرير والأرباح السنوية لعام {targetYear} (من 01/01/{targetYear} إلى 31/12/{targetYear})";
                        break;
                    case "فترة مخصصة":
                        startDate = CustomStartDate.Date;
                        endDate = CustomEndDate.Date.AddDays(1).AddTicks(-1);
                        DateRange = $"من {startDate:dd/MM/yyyy} إلى {CustomEndDate:dd/MM/yyyy}";
                        break;
                    default:
                        startDate = DateTime.Today.AddDays(-29);
                        endDate = DateTime.Now;
                        DateRange = $"{startDate.ToString("d MMMM", arCulture)} - {now.ToString("d MMMM yyyy", arCulture)}";
                        break;
                }
                
                var allSales = await _saleRepository.GetAllAsync();
                var filteredSales = allSales.Where(s => s.Date >= startDate && s.Date <= endDate).ToList();
                
                var allProducts = await _productRepository.GetAllAsync();
                var allExpenses = await _expenseRepository.GetAllAsync();
                var allReturns = await _returnRepository.GetAllAsync();
                var allCategories = await _categoryRepository.GetAllAsync();
                var allLineItems = await _lineItemRepository.GetAllAsync();

                var periodExpenses = allExpenses.Where(e => e.ExpenseDate.DateTime >= startDate && e.ExpenseDate.DateTime <= endDate).ToList();
                var periodReturns = allReturns.Where(r => r.ReturnDate.DateTime >= startDate && r.ReturnDate.DateTime <= endDate).ToList();

                TotalSales = filteredSales.Sum(s => s.Total);
                TotalExpenses = periodExpenses.Sum(e => e.Amount);
                TotalReturns = periodReturns.Sum(r => r.TotalRefundAmount);
                TotalProfit = TotalSales - TotalReturns - TotalExpenses;
                
                TotalProducts = allProducts.Count;
                TotalTransactions = filteredSales.Count;

                var lowStockThreshold = 10;
                var lowStockProducts = allProducts.Where(p => p.CurrentStock < lowStockThreshold).ToList();
                LowStockCount = lowStockProducts.Count;

                // ==================== 1. DAILY / MONTHLY SALES CHART DATA ====================
                DailySalesChartData.Clear();

                var rawGroups = new List<(string Label, decimal Amount)>();

                if (SelectedDateRange == "سنوي")
                {
                    var monthNames = new[] { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };
                    int year = startDate.Year;

                    for (int m = 1; m <= 12; m++)
                    {
                        var mSales = filteredSales
                            .Where(s => s.Date.Year == year && s.Date.Month == m)
                            .Sum(s => s.Total);
                        rawGroups.Add((monthNames[m - 1], mSales));
                    }
                }
                else
                {
                    int daysCount = SelectedDateRange switch
                    {
                        "اليوم" => 1,
                        "آخر 7 أيام" => 7,
                        "آخر 30 يوم" => 14,
                        _ => 7
                    };

                    var chartStartDate = SelectedDateRange == "اليوم" ? DateTime.Today : DateTime.Today.AddDays(-(daysCount - 1));

                    for (int i = 0; i < daysCount; i++)
                    {
                        var day = chartStartDate.AddDays(i);
                        var daySales = filteredSales
                            .Where(s => s.Date.Date == day.Date)
                            .Sum(s => s.Total);
                        var label = SelectedDateRange == "اليوم" 
                            ? day.ToString("d MMMM", arCulture) 
                            : day.ToString("dd/MM");
                        rawGroups.Add((label, daySales));
                    }
                }

                decimal maxSaleVal = rawGroups.Count > 0 ? rawGroups.Max(g => g.Amount) : 0m;
                ChartYAxisLabelMax = $"{maxSaleVal:#,##0.##} ج.م";
                ChartYAxisLabelMid = $"{(maxSaleVal / 2m):#,##0.##} ج.م";

                decimal calcMax = maxSaleVal > 0 ? maxSaleVal : 1m;

                int totalNodes = rawGroups.Count;
                double canvasW = 560.0;
                double canvasH = 150.0;
                double topY = 20.0;
                double bottomY = 135.0;
                double availH = bottomY - topY;

                var linePoints = new List<System.Windows.Point>();

                for (int idx = 0; idx < totalNodes; idx++)
                {
                    var g = rawGroups[idx];
                    double px = totalNodes == 1 ? 280.0 : 25.0 + (idx * ((canvasW - 50.0) / (totalNodes - 1)));
                    double ratio = (double)(g.Amount / calcMax);
                    double py = bottomY - (ratio * availH);

                    linePoints.Add(new System.Windows.Point(px, py));

                    DailySalesChartData.Add(new DailySalesBarItem
                    {
                        DayLabel = g.Label,
                        Amount = g.Amount,
                        BarHeight = ratio * 120.0,
                        BarHeightPercentage = ratio * 100.0,
                        PointX = px,
                        PointY = py
                    });
                }

                // Build Line and Area Path Geometries
                if (linePoints.Count > 0)
                {
                    var lineSb = new System.Text.StringBuilder();
                    var areaSb = new System.Text.StringBuilder();

                    lineSb.Append(CultureInfo.InvariantCulture, $"M {linePoints[0].X:F1},{linePoints[0].Y:F1}");
                    areaSb.Append(CultureInfo.InvariantCulture, $"M {linePoints[0].X:F1},{bottomY + 10:F1} L {linePoints[0].X:F1},{linePoints[0].Y:F1}");

                    for (int i = 1; i < linePoints.Count; i++)
                    {
                        lineSb.Append(CultureInfo.InvariantCulture, $" L {linePoints[i].X:F1},{linePoints[i].Y:F1}");
                        areaSb.Append(CultureInfo.InvariantCulture, $" L {linePoints[i].X:F1},{linePoints[i].Y:F1}");
                    }

                    areaSb.Append(CultureInfo.InvariantCulture, $" L {linePoints[^1].X:F1},{bottomY + 10:F1} Z");

                    SalesLinePathData = lineSb.ToString();
                    SalesAreaPathData = areaSb.ToString();
                }

                // ==================== 2. CATEGORY REVENUE DISTRIBUTION ====================
                CategoryDistributionData.Clear();
                var saleIds = filteredSales.Select(s => s.Id).ToHashSet();
                var periodLineItems = allLineItems.Where(li => saleIds.Contains(li.SaleId)).ToList();
                var grandLineTotal = periodLineItems.Sum(li => li.LineTotal);

                var productMap = allProducts.ToDictionary(p => p.Id, p => p);
                var catMap = allCategories.ToDictionary(c => c.Id, c => string.IsNullOrEmpty(c.NameAR) ? c.NameEN : c.NameAR);

                var catGroups = periodLineItems.GroupBy(li =>
                {
                    if (productMap.TryGetValue(li.ProductId, out var p))
                        return p.CategoryId;
                    return 0;
                }).OrderByDescending(g => g.Sum(li => li.LineTotal)).Take(4).ToList();

                var colors = new[] { "#D90429", "#EF233C", "#B90320", "#FF4D6D", "#C9184A" };
                int cIdx = 0;

                foreach (var cg in catGroups)
                {
                    var catId = cg.Key;
                    var catName = catMap.TryGetValue(catId, out var cn) ? cn : (catId == 0 ? "عام" : $"قسم #{catId}");
                    var catTotal = cg.Sum(li => li.LineTotal);
                    var pct = grandLineTotal > 0 ? (double)(catTotal / grandLineTotal * 100) : 0;

                    CategoryDistributionData.Add(new CategoryRevenueDistributionItem
                    {
                        CategoryName = catName,
                        TotalSales = catTotal,
                        Percentage = pct,
                        ColorHex = colors[cIdx % colors.Length]
                    });
                    cIdx++;
                }

                // ==================== 3. RECENT SALES ====================
                RecentSales.Clear();
                foreach (var s in filteredSales.OrderByDescending(x => x.Date).Take(6))
                {
                    string statusAr = s.Status switch
                    {
                        "Completed" => "مكتملة",
                        "Cancelled" => "ملغاة",
                        "Pending" => "قيد الانتظار",
                        "Refunded" => "مرتجع",
                        _ => s.Status ?? "مكتملة"
                    };

                    RecentSales.Add(new RecentSaleItem 
                    { 
                        TxId = !string.IsNullOrEmpty(s.ReceiptNumber) ? s.ReceiptNumber : $"#{s.Id}", 
                        Customer = "عميل نقدي", 
                        AmountDisplay = $"{s.Total:#,##0.##} ج.م", 
                        Status = statusAr, 
                        Time = s.Date.ToString("yyyy/MM/dd  hh:mm tt", arCulture)
                    });
                }

                // ==================== 4. INVENTORY ALERTS ====================
                InventoryAlerts.Clear();
                foreach (var p in lowStockProducts.Take(5))
                {
                    string alertMsg = p.CurrentStock <= 0 ? "نفذت الكمية بالكامل بالمخزن!" : $"المتبقي {(int)p.CurrentStock} قطع فقط";
                    InventoryAlerts.Add(new InventoryAlertItem 
                    { 
                        ProductName = string.IsNullOrEmpty(p.NameAR) ? p.NameEN : p.NameAR, 
                        Alert = alertMsg, 
                        Icon = "AlertCircleOutline" 
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard load failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class DailySalesBarItem
    {
        public string DayLabel { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double BarHeight { get; set; } = 8;
        public double BarHeightPercentage { get; set; }
        public string AmountDisplay => $"{Amount:#,##0.##} ج.م";
        
        public double PointX { get; set; } = 30;
        public double PointY { get; set; } = 140;
        public double CanvasLeft => PointX - 6;
        public double CanvasTop => PointY - 6;
    }

    public class CategoryRevenueDistributionItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public double Percentage { get; set; }
        public string PercentageDisplay => Percentage % 1 == 0 ? $"{Percentage:0}%" : $"{Percentage:0.#}%";
        public string ColorHex { get; set; } = "#D90429";
    }

    public class RecentSaleItem
    {
        public string TxId { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string AmountDisplay { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }

    public class InventoryAlertItem
    {
        public string ProductName { get; set; } = string.Empty;
        public string Alert { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
