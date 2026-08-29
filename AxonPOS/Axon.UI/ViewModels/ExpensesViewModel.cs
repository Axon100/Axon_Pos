using Axon.Application.Interfaces.Repositories;
using Axon.UI.Views;
using Axon.Domain.Entities;
using Axon.UI.Services;
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
    public partial class ExpensesViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isCardViewMode = true; // Default to 3D Cards Box View

        [RelayCommand]
        private void SetCardViewMode() => IsCardViewMode = true;

        [RelayCommand]
        private void SetTableViewMode() => IsCardViewMode = false;

        [ObservableProperty]
        private DateTime _dateFrom = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime _dateTo = DateTime.Today;

        [ObservableProperty]
        private int _totalExpensesCount;

        [ObservableProperty]
        private decimal _totalExpensesAmount;

        [ObservableProperty]
        private string _topExpenseCategory = "—";

        [ObservableProperty]
        private ObservableCollection<ExpenseItemViewModel> _expenses = new();

        [ObservableProperty]
        private ObservableCollection<ExpenseItemViewModel> _filteredExpenses = new();

        private readonly IRepository<Expense> _expenseRepository;

        public ExpensesViewModel(IRepository<Expense> expenseRepository)
        {
            _expenseRepository = expenseRepository;

            Title = "المصروفات الخارجية والنفقات";
            _ = LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                Expenses.Clear();
                var dbExpenses = await _expenseRepository.GetAllAsync();

                var start = DateFrom.Date;
                var end = DateTo.Date.AddDays(1).AddTicks(-1);

                var periodExpenses = dbExpenses
                    .Where(e => e.ExpenseDate.DateTime >= start && e.ExpenseDate.DateTime <= end)
                    .OrderByDescending(x => x.ExpenseDate)
                    .ToList();

                foreach (var e in periodExpenses)
                {
                    Expenses.Add(new ExpenseItemViewModel
                    {
                        Id = e.Id,
                        DocNumber = string.IsNullOrEmpty(e.ReferenceNumber) ? $"EXP-{e.Id:D4}" : e.ReferenceNumber,
                        Category = string.IsNullOrEmpty(e.Category) ? "عام" : e.Category,
                        PaymentMethod = "نقداً (Cash)",
                        Date = e.ExpenseDate.ToString("yyyy/MM/dd HH:mm"),
                        DateRaw = e.ExpenseDate.DateTime,
                        Amount = e.Amount,
                        Description = string.IsNullOrEmpty(e.Description) ? "مصروف عام" : e.Description
                    });
                }

                OnSearch();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load expenses: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddExpenseAsync()
        {
            try
            {
                var dialog = new Axon.UI.Views.AddExpenseWindow();
                if (dialog.ShowDialog() == true && dialog.Result != null)
                {
                    var newExpense = new Expense
                    {
                        Category = string.IsNullOrEmpty(dialog.Result.Category) ? "نثريات ومصروفات" : dialog.Result.Category,
                        Description = string.IsNullOrEmpty(dialog.Result.Description) ? "مصروف خارجي" : dialog.Result.Description,
                        Amount = dialog.Result.Amount,
                        ExpenseDate = DateTimeOffset.Now,
                        ReferenceNumber = dialog.Result.DocNumber,
                        UserId = UserSessionService.CurrentUserId > 0 ? UserSessionService.CurrentUserId : 1
                    };

                    await _expenseRepository.AddAsync(newExpense);
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                AxonMessageBox.Show($"فشل فتح نافذة المصروفات: {ex.Message}", "خطأ في التشغيل", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public bool IsAdmin => UserSessionService.IsAdmin;

        [RelayCommand]
        private async Task DeleteExpenseAsync(ExpenseItemViewModel item)
        {
            if (item == null) return;

            if (!UserSessionService.IsAdmin && !UserSessionService.HasPermission("Expenses.Delete"))
            {
                AxonMessageBox.Show("عذراً، خاصية حذف المصروفات مخصصة لمدير النظام (Admin) فقط!", "تنبيه الصلاحيات", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var confirm = AxonMessageBox.Show(
                $"هل أنت متأكد من حذف قيد المصروف:\n\n• المستند: {item.DocNumber}\n• البيان: {item.Description}\n• المبلغ: {item.Amount:N0} ج.م\n\nلن يمكن التراجع عن هذه الخطوة!",
                "تأكيد حذف المصروف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var expense = await _expenseRepository.GetByIdAsync(item.Id);
                if (expense != null)
                {
                    await _expenseRepository.DeleteAsync(expense);
                    await LoadDataAsync();
                    AxonMessageBox.Show("تم حذف قيد المصروف بنجاح!", "نجاح العملية", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                AxonMessageBox.Show($"فشل حذف قيد المصروف: {ex.Message}", "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            OnSearch();
        }

        partial void OnDateFromChanged(DateTime value)
        {
            _ = LoadDataAsync();
        }

        partial void OnDateToChanged(DateTime value)
        {
            _ = LoadDataAsync();
        }

        private void OnSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredExpenses = new ObservableCollection<ExpenseItemViewModel>(Expenses);
            }
            else
            {
                var filtered = Expenses.Where(x => 
                    x.DocNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    x.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    x.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                FilteredExpenses = new ObservableCollection<ExpenseItemViewModel>(filtered);
            }
            
            TotalExpensesCount = FilteredExpenses.Count;
            TotalExpensesAmount = FilteredExpenses.Sum(x => x.Amount);

            var topCat = FilteredExpenses.GroupBy(x => x.Category).OrderByDescending(g => g.Sum(x => x.Amount)).FirstOrDefault();
            TopExpenseCategory = topCat != null ? topCat.Key : "—";
        }
    }

    public class ExpenseItemViewModel
    {
        public int Id { get; set; }
        public string DocNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public DateTime DateRaw { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
