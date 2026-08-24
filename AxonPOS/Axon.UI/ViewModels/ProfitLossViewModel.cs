using Axon.Application.Interfaces.Repositories;
using Axon.Domain.Entities;
using Axon.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Axon.UI.ViewModels
{
    public partial class ProfitLossViewModel : BaseViewModel
    {
        private readonly IRepository<Product> _productRepository;

        // ===== Selection Timeframe for Cards =====
        [ObservableProperty]
        private string _profitTimeframe = "يومي";

        [ObservableProperty]
        private string _lossTimeframe = "يومي";

        // ===== profit and loss card dynamic values =====
        [ObservableProperty]
        private decimal _totalProfitAmount;

        [ObservableProperty]
        private decimal _totalLossAmount;

        // ===== Inventory Audit Filter =====
        [ObservableProperty]
        private string _auditPeriod = "يومي";

        [ObservableProperty]
        private string _auditDateDisplay = DateTime.Now.ToString("yyyy-MM-dd");

        // ===== Audit Metrics =====
        [ObservableProperty]
        private int _totalItemsSold;

        [ObservableProperty]
        private decimal _salesRevenue;

        [ObservableProperty]
        private decimal _salesCost;

        [ObservableProperty]
        private decimal _auditExpenses;

        [ObservableProperty]
        private decimal _netProfit;

        // ===== Grid Collections =====
        [ObservableProperty]
        private ObservableCollection<AuditItemViewModel> _auditItems = new();

        [ObservableProperty]
        private ObservableCollection<InventoryStockLogViewModel> _stockLogs = new();

        public ICommand UpdateProfitTimeframeCommand { get; }
        public ICommand UpdateLossTimeframeCommand { get; }
        public ICommand UpdateAuditPeriodCommand { get; }
        public ICommand AddStockCommand { get; }
        public ICommand RefreshCommand { get; }

        public ProfitLossViewModel(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;

            UpdateProfitTimeframeCommand = new RelayCommand<string>(OnUpdateProfitTimeframe);
            UpdateLossTimeframeCommand = new RelayCommand<string>(OnUpdateLossTimeframe);
            UpdateAuditPeriodCommand = new RelayCommand<string>(OnUpdateAuditPeriod);
            AddStockCommand = new RelayCommand(OnAddStock);
            RefreshCommand = new AsyncRelayCommand(LoadFromDatabaseAsync);

            _ = LoadFromDatabaseAsync();
        }

        private async Task LoadFromDatabaseAsync()
        {
            IsBusy = true;
            try
            {
                AuditItems.Clear();
                StockLogs.Clear();

                var products = await _productRepository.GetAllAsync();
                var productList = products.ToList();

                if (productList.Count == 0)
                {
                    // No products yet - show empty state
                    TotalItemsSold = 0;
                    SalesRevenue = 0;
                    SalesCost = 0;
                    AuditExpenses = 0;
                    NetProfit = 0;
                    TotalProfitAmount = 0;
                    TotalLossAmount = 0;
                    return;
                }

                // Build audit items from actual products in DB
                foreach (var p in productList)
                {
                    string name = string.IsNullOrEmpty(p.NameAR) ? p.NameEN : p.NameAR;
                    decimal profit = (p.SellingPrice - p.CostPrice) * p.CurrentStock;

                    AuditItems.Add(new AuditItemViewModel
                    {
                        ItemName = name,
                        SKU = p.SKU,
                        SoldQty = 0,  // Actual sales data would come from SaleItems table
                        SoldPrice = p.SellingPrice,
                        TotalSales = 0,
                        TotalCost = p.CostPrice * p.CurrentStock,
                        Profit = profit > 0 ? profit : 0,
                        CurrentStock = (int)p.CurrentStock
                    });
                }

                // Compute totals from actual DB data
                decimal totalCost = productList.Sum(p => p.CostPrice * p.CurrentStock);
                decimal totalValue = productList.Sum(p => p.SellingPrice * p.CurrentStock);
                decimal totalPotentialProfit = totalValue - totalCost;

                SalesRevenue = totalValue;
                SalesCost = totalCost;
                AuditExpenses = 0;
                NetProfit = totalPotentialProfit;
                TotalItemsSold = productList.Sum(p => (int)p.CurrentStock);

                // Set timeframe-based card values
                TotalProfitAmount = totalPotentialProfit > 0 ? totalPotentialProfit : 0;
                TotalLossAmount = 0;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnUpdateProfitTimeframe(string? timeframe)
        {
            if (string.IsNullOrEmpty(timeframe)) return;
            ProfitTimeframe = timeframe;
            // In a full implementation, these would query filtered DB data by date range
            if (timeframe == "يومي") TotalProfitAmount = NetProfit / 30;
            else if (timeframe == "أسبوعي") TotalProfitAmount = NetProfit / 4;
            else TotalProfitAmount = NetProfit;
        }

        private void OnUpdateLossTimeframe(string? timeframe)
        {
            if (string.IsNullOrEmpty(timeframe)) return;
            LossTimeframe = timeframe;
            if (timeframe == "يومي") TotalLossAmount = AuditExpenses / 30;
            else if (timeframe == "أسبوعي") TotalLossAmount = AuditExpenses / 4;
            else TotalLossAmount = AuditExpenses;
        }

        private void OnUpdateAuditPeriod(string? period)
        {
            if (string.IsNullOrEmpty(period)) return;
            AuditPeriod = period;
            if (period == "يومي") AuditDateDisplay = DateTime.Now.ToString("yyyy-MM-dd");
            else if (period == "شهري") AuditDateDisplay = DateTime.Now.ToString("yyyy-MM");
            else AuditDateDisplay = DateTime.Now.ToString("yyyy");
        }

        private void OnAddStock()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                var dialog = new Axon.UI.Views.AddStockWindow();
                if (dialog.ShowDialog() == true && dialog.Result != null)
                {
                    StockLogs.Insert(0, dialog.Result);

                    // Find the product in DB by name and increment its stock
                    var products = await _productRepository.GetAllAsync();
                    var match = products.FirstOrDefault(p =>
                        (p.NameAR ?? "").Contains(dialog.Result.ItemName) ||
                        (p.NameEN ?? "").Contains(dialog.Result.ItemName) ||
                        p.SKU.Contains(dialog.Result.ItemName));

                    if (match != null)
                    {
                        match.CurrentStock += dialog.Result.QuantityAdded;
                        await _productRepository.UpdateAsync(match);
                    }

                    // Refresh the audit grid from DB
                    await LoadFromDatabaseAsync();
                }
            });
        }
    }

    public class AuditItemViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int SoldQty { get; set; }
        public decimal SoldPrice { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit { get; set; }
        public int CurrentStock { get; set; }
    }

    public class InventoryStockLogViewModel
    {
        public int? ProductId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string DocNumber { get; set; } = string.Empty;
        public int QuantityAdded { get; set; }
        public string Date { get; set; } = string.Empty;
    }
}

