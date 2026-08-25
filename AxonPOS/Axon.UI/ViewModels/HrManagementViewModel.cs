using Axon.Application.Interfaces.Repositories;
using Axon.Domain.Entities;
using Axon.UI.Helpers;
using Axon.UI.Services;
using Axon.UI.Views;
using Axon.UI.ViewModels.Base;
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
    public partial class HrManagementViewModel : BaseViewModel
    {
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<EmployeeAdvance> _advanceRepository;
        private readonly IRepository<EmployeeAdvancePayment> _advancePaymentRepository;
        private readonly IRepository<EmployeeSalaryPayment> _salaryPaymentRepository;
        private readonly IRepository<EmployeeAttendance> _attendanceRepository;
        private readonly IRepository<EmployeeDeduction> _deductionRepository;
        private readonly IRepository<EmployeeLeave> _leaveRepository;

        // Active Selected Sub-Tab (0 to 8)
        [ObservableProperty]
        private int _selectedTab = 0;

        // KPI Summary Boxes
        [ObservableProperty]
        private int _totalEmployeesCount;

        [ObservableProperty]
        private decimal _totalMonthlySalaryPool;

        [ObservableProperty]
        private decimal _totalPendingAdvances;

        [ObservableProperty]
        private decimal _totalDeductionsThisMonth;

        [ObservableProperty]
        private int _todayPresentCount;

        // Admin Date Filter Controls
        [ObservableProperty]
        private DateTime _filterFromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        [ObservableProperty]
        private DateTime _filterToDate = DateTime.Today;

        [ObservableProperty]
        private Employee? _selectedFilterEmployee;

        // Master Collections
        public ObservableCollection<Employee> Employees { get; } = new();
        public ObservableCollection<EmployeeAdvance> Advances { get; } = new();
        public ObservableCollection<EmployeeAdvancePayment> AdvancePayments { get; } = new();
        public ObservableCollection<EmployeeSalaryPayment> SalaryPayments { get; } = new();
        public ObservableCollection<EmployeeAttendance> Attendances { get; } = new();
        public ObservableCollection<EmployeeDeduction> Deductions { get; } = new();
        public ObservableCollection<EmployeeLeave> Leaves { get; } = new();

        // Forms & Dialog Inputs
        // 1. Employee Dialog
        [ObservableProperty]
        private bool _isEmployeeDialogOpen;

        [ObservableProperty]
        private bool _isEditEmployeeMode;

        [ObservableProperty]
        private int? _editingEmployeeId;

        [ObservableProperty]
        private string _empFullName = string.Empty;

        [ObservableProperty]
        private string _empJobTitle = string.Empty;

        [ObservableProperty]
        private string _empPhone = string.Empty;

        [ObservableProperty]
        private string _empNationalId = string.Empty;

        [ObservableProperty]
        private decimal _empBasicSalary;

        // 2. Advance Dialog
        [ObservableProperty]
        private bool _isAdvanceDialogOpen;

        [ObservableProperty]
        private Employee? _selectedAdvanceEmployee;

        [ObservableProperty]
        private decimal _advanceAmount;

        [ObservableProperty]
        private DateTime _advanceDate = DateTime.Today;

        [ObservableProperty]
        private string _advanceNotes = string.Empty;

        // 3. Advance Payoff Dialog
        [ObservableProperty]
        private bool _isPayoffDialogOpen;

        [ObservableProperty]
        private EmployeeAdvance? _selectedAdvanceToPay;

        [ObservableProperty]
        private decimal _payoffAmount;

        [ObservableProperty]
        private DateTime _payoffDate = DateTime.Today;

        [ObservableProperty]
        private string _payoffNotes = string.Empty;

        // 4. Salary Payment Dialog
        [ObservableProperty]
        private bool _isSalaryPaymentDialogOpen;

        [ObservableProperty]
        private Employee? _selectedSalaryEmployee;

        [ObservableProperty]
        private int _salaryMonth = DateTime.Today.Month;

        [ObservableProperty]
        private int _salaryYear = DateTime.Today.Year;

        [ObservableProperty]
        private decimal _salaryBasic;

        [ObservableProperty]
        private decimal _salaryBonus;

        [ObservableProperty]
        private decimal _salaryDeductions;

        [ObservableProperty]
        private decimal _salaryAdvanceDeduction;

        [ObservableProperty]
        private decimal _salaryNet;

        [ObservableProperty]
        private string _salaryNotes = string.Empty;

        // 5. Attendance Dialog
        [ObservableProperty]
        private bool _isAttendanceDialogOpen;

        [ObservableProperty]
        private Employee? _selectedAttendanceEmployee;

        [ObservableProperty]
        private DateTime _attendanceDate = DateTime.Today;

        [ObservableProperty]
        private TimeSpan _checkInTime = new TimeSpan(9, 0, 0);

        [ObservableProperty]
        private TimeSpan _checkOutTime = new TimeSpan(17, 0, 0);

        [ObservableProperty]
        private string _attendanceStatus = "حاضر";

        [ObservableProperty]
        private string _attendanceNotes = string.Empty;

        // 6. Deduction Dialog
        [ObservableProperty]
        private bool _isDeductionDialogOpen;

        [ObservableProperty]
        private Employee? _selectedDeductionEmployee;

        [ObservableProperty]
        private DateTime _deductionDate = DateTime.Today;

        [ObservableProperty]
        private decimal _deductionAmount;

        [ObservableProperty]
        private string _deductionReason = string.Empty;

        [ObservableProperty]
        private string _deductionNotes = string.Empty;

        // 7. Leave Dialog
        [ObservableProperty]
        private bool _isLeaveDialogOpen;

        [ObservableProperty]
        private Employee? _selectedLeaveEmployee;

        [ObservableProperty]
        private DateTime _leaveStartDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _leaveEndDate = DateTime.Today;

        [ObservableProperty]
        private string _leaveType = "إجازة إعتيادية";

        [ObservableProperty]
        private string _leaveReason = string.Empty;

        public HrManagementViewModel(
            IRepository<Employee> employeeRepository,
            IRepository<EmployeeAdvance> advanceRepository,
            IRepository<EmployeeAdvancePayment> advancePaymentRepository,
            IRepository<EmployeeSalaryPayment> salaryPaymentRepository,
            IRepository<EmployeeAttendance> attendanceRepository,
            IRepository<EmployeeDeduction> deductionRepository,
            IRepository<EmployeeLeave> leaveRepository)
        {
            _employeeRepository = employeeRepository;
            _advanceRepository = advanceRepository;
            _advancePaymentRepository = advancePaymentRepository;
            _salaryPaymentRepository = salaryPaymentRepository;
            _attendanceRepository = attendanceRepository;
            _deductionRepository = deductionRepository;
            _leaveRepository = leaveRepository;

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                var empList = (await _employeeRepository.GetAllAsync()).Where(e => !e.IsDeleted).ToList();
                Employees.Clear();
                foreach (var e in empList) Employees.Add(e);

                var advList = (await _advanceRepository.GetAllAsync()).Where(a => !a.IsDeleted).ToList();
                Advances.Clear();
                foreach (var a in advList) Advances.Add(a);

                var payList = (await _advancePaymentRepository.GetAllAsync()).Where(p => !p.IsDeleted).ToList();
                AdvancePayments.Clear();
                foreach (var p in payList) AdvancePayments.Add(p);

                var salList = (await _salaryPaymentRepository.GetAllAsync()).Where(s => !s.IsDeleted).ToList();
                SalaryPayments.Clear();
                foreach (var s in salList) SalaryPayments.Add(s);

                var attList = (await _attendanceRepository.GetAllAsync()).Where(at => !at.IsDeleted).ToList();
                Attendances.Clear();
                foreach (var at in attList) Attendances.Add(at);

                var dedList = (await _deductionRepository.GetAllAsync()).Where(d => !d.IsDeleted).ToList();
                Deductions.Clear();
                foreach (var d in dedList) Deductions.Add(d);

                var levList = (await _leaveRepository.GetAllAsync()).Where(l => !l.IsDeleted).ToList();
                Leaves.Clear();
                foreach (var l in levList) Leaves.Add(l);

                // Update KPI Cards
                TotalEmployeesCount = Employees.Count(e => e.IsActive);
                TotalMonthlySalaryPool = Employees.Where(e => e.IsActive).Sum(e => e.BasicSalary);
                TotalPendingAdvances = Advances.Sum(a => a.RemainingAmount);
                TotalDeductionsThisMonth = Deductions.Where(d => d.DeductionDate.Month == DateTime.Today.Month && d.DeductionDate.Year == DateTime.Today.Year).Sum(d => d.Amount);
                TodayPresentCount = Attendances.Count(a => a.Date.Date == DateTime.Today.Date && a.Status == "حاضر");
            }
            catch (Exception ex)
            {
                Axon.UI.Views.AxonMessageBox.Show($"خطأ أثناء تحميل بيانات شؤون العاملين: {ex.Message}", "خطأ في التحميل", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task FilterByDateRangeAsync()
        {
            await LoadDataAsync();
        }

        // ==================== 1. EMPLOYEE CRUD ====================
        [RelayCommand]
        private void OpenAddEmployeeDialog()
        {
            IsEditEmployeeMode = false;
            EditingEmployeeId = null;
            EmpFullName = string.Empty;
            EmpJobTitle = string.Empty;
            EmpPhone = string.Empty;
            EmpNationalId = string.Empty;
            EmpBasicSalary = 0;
            IsEmployeeDialogOpen = true;
        }

        [RelayCommand]
        private void OpenEditEmployeeDialog(Employee emp)
        {
            if (emp == null) return;
            IsEditEmployeeMode = true;
            EditingEmployeeId = emp.Id;
            EmpFullName = emp.FullName;
            EmpJobTitle = emp.JobTitle;
            EmpPhone = emp.Phone;
            EmpNationalId = emp.NationalId;
            EmpBasicSalary = emp.BasicSalary;
            IsEmployeeDialogOpen = true;
        }

        [RelayCommand]
        private void CloseEmployeeDialog() => IsEmployeeDialogOpen = false;

        [RelayCommand]
        private async Task SaveEmployeeAsync()
        {
            if (string.IsNullOrWhiteSpace(EmpFullName))
            {
                Axon.UI.Views.AxonMessageBox.Show("يرجى إدخال اسم الموظف!", "حقل مطلوب", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditEmployeeMode && EditingEmployeeId.HasValue)
                {
                    var emp = await _employeeRepository.GetByIdAsync(EditingEmployeeId.Value);
                    if (emp != null)
                    {
                        emp.FullName = EmpFullName;
                        emp.JobTitle = EmpJobTitle;
                        emp.Phone = EmpPhone;
                        emp.NationalId = EmpNationalId;
                        emp.BasicSalary = EmpBasicSalary;
                        await _employeeRepository.UpdateAsync(emp);
                    }
                }
                else
                {
                    var newEmp = new Employee
                    {
                        FullName = EmpFullName,
                        JobTitle = EmpJobTitle,
                        Phone = EmpPhone,
                        NationalId = EmpNationalId,
                        BasicSalary = EmpBasicSalary,
                        HireDate = DateTime.Today,
                        IsActive = true
                    };
                    await _employeeRepository.AddAsync(newEmp);
                }

                IsEmployeeDialogOpen = false;
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== 2. ADVANCE REGISTRATION ====================
        [RelayCommand]
        private void OpenAddAdvanceDialog()
        {
            SelectedAdvanceEmployee = Employees.FirstOrDefault();
            AdvanceAmount = 0;
            AdvanceDate = DateTime.Today;
            AdvanceNotes = string.Empty;
            IsAdvanceDialogOpen = true;
        }

        [RelayCommand]
        private void CloseAdvanceDialog() => IsAdvanceDialogOpen = false;

        [RelayCommand]
        private async Task SaveAdvanceAsync()
        {
            if (SelectedAdvanceEmployee == null || AdvanceAmount <= 0)
            {
                Axon.UI.Views.AxonMessageBox.Show("يرجى اختيار الموظف وإدخال قيمة السلفة بشكل صحيح!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var adv = new EmployeeAdvance
                {
                    EmployeeId = SelectedAdvanceEmployee.Id,
                    Amount = AdvanceAmount,
                    PaidAmount = 0,
                    RemainingAmount = AdvanceAmount,
                    AdvanceDate = AdvanceDate,
                    Notes = AdvanceNotes,
                    Status = "غير مسددة"
                };

                await _advanceRepository.AddAsync(adv);
                IsAdvanceDialogOpen = false;
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== 3. ADVANCE PAYOFF / SETTLEMENT ====================
        [RelayCommand]
        private void OpenPayoffDialog(EmployeeAdvance adv)
        {
            if (adv == null || adv.RemainingAmount <= 0)
            {
                Axon.UI.Views.AxonMessageBox.Show("هذه السلفة مسددة بالكامل بالفعل!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedAdvanceToPay = adv;
            PayoffAmount = adv.RemainingAmount;
            PayoffDate = DateTime.Today;
            PayoffNotes = string.Empty;
            IsPayoffDialogOpen = true;
        }

        [RelayCommand]
        private void ClosePayoffDialog() => IsPayoffDialogOpen = false;

        [RelayCommand]
        private async Task SavePayoffAsync()
        {
            if (SelectedAdvanceToPay == null || PayoffAmount <= 0)
            {
                Axon.UI.Views.AxonMessageBox.Show("يرجى إدخال مبلغ سداد صحيح!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PayoffAmount > SelectedAdvanceToPay.RemainingAmount)
            {
                Axon.UI.Views.AxonMessageBox.Show("مبلغ السداد أكبر من قيمة السلفة المتبقية!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var pmt = new EmployeeAdvancePayment
                {
                    EmployeeAdvanceId = SelectedAdvanceToPay.Id,
                    PaymentDate = PayoffDate,
                    AmountPaid = PayoffAmount,
                    Notes = PayoffNotes
                };
                await _advancePaymentRepository.AddAsync(pmt);

                SelectedAdvanceToPay.PaidAmount += PayoffAmount;
                SelectedAdvanceToPay.RemainingAmount = SelectedAdvanceToPay.Amount - SelectedAdvanceToPay.PaidAmount;
                SelectedAdvanceToPay.Status = SelectedAdvanceToPay.RemainingAmount <= 0 ? "مسددة بالكامل" : "سداد جزئي";

                await _advanceRepository.UpdateAsync(SelectedAdvanceToPay);
                IsPayoffDialogOpen = false;
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== 4. SALARY PAYMENT ====================
        [RelayCommand]
        private void OpenSalaryPaymentDialog(Employee? emp = null)
        {
            SelectedSalaryEmployee = emp ?? Employees.FirstOrDefault();
            SalaryMonth = DateTime.Today.Month;
            SalaryYear = DateTime.Today.Year;
            CalculateSalaryBreakdown();
            IsSalaryPaymentDialogOpen = true;
        }

        partial void OnSelectedSalaryEmployeeChanged(Employee? value) => CalculateSalaryBreakdown();
        partial void OnSalaryMonthChanged(int value) => CalculateSalaryBreakdown();
        partial void OnSalaryYearChanged(int value) => CalculateSalaryBreakdown();
        partial void OnSalaryBonusChanged(decimal value) => CalculateNetSalary();
        partial void OnSalaryDeductionsChanged(decimal value) => CalculateNetSalary();
        partial void OnSalaryAdvanceDeductionChanged(decimal value) => CalculateNetSalary();

        private void CalculateSalaryBreakdown()
        {
            if (SelectedSalaryEmployee == null) return;
            SalaryBasic = SelectedSalaryEmployee.BasicSalary;

            // Auto fetch deductions for selected month
            var mDeds = Deductions.Where(d => d.EmployeeId == SelectedSalaryEmployee.Id && d.DeductionDate.Month == SalaryMonth && d.DeductionDate.Year == SalaryYear).Sum(d => d.Amount);
            SalaryDeductions = mDeds;

            // Auto suggest advance loan deduction
            var empAdv = Advances.Where(a => a.EmployeeId == SelectedSalaryEmployee.Id && a.RemainingAmount > 0).Sum(a => a.RemainingAmount);
            SalaryAdvanceDeduction = Math.Min(empAdv, SalaryBasic * 0.5m); // suggest max 50% basic or remaining loan

            CalculateNetSalary();
        }

        private void CalculateNetSalary()
        {
            SalaryNet = SalaryBasic + SalaryBonus - SalaryDeductions - SalaryAdvanceDeduction;
        }

        [RelayCommand]
        private void CloseSalaryPaymentDialog() => IsSalaryPaymentDialogOpen = false;

        [RelayCommand]
        private async Task SaveSalaryPaymentAsync()
        {
            if (SelectedSalaryEmployee == null) return;

            IsBusy = true;
            try
            {
                var sal = new EmployeeSalaryPayment
                {
                    EmployeeId = SelectedSalaryEmployee.Id,
                    PaymentDate = DateTime.Today,
                    Month = SalaryMonth,
                    Year = SalaryYear,
                    BasicSalary = SalaryBasic,
                    BonusAmount = SalaryBonus,
                    DeductionAmount = SalaryDeductions,
                    AdvanceDeduction = SalaryAdvanceDeduction,
                    NetSalary = SalaryNet,
                    Notes = SalaryNotes
                };

                await _salaryPaymentRepository.AddAsync(sal);

                // If advance deduction was applied, settle advances automatically
                if (SalaryAdvanceDeduction > 0)
                {
                    var openAdvances = Advances.Where(a => a.EmployeeId == SelectedSalaryEmployee.Id && a.RemainingAmount > 0).OrderBy(a => a.AdvanceDate).ToList();
                    decimal remDeduct = SalaryAdvanceDeduction;
                    foreach (var adv in openAdvances)
                    {
                        if (remDeduct <= 0) break;
                        decimal payThis = Math.Min(adv.RemainingAmount, remDeduct);
                        adv.PaidAmount += payThis;
                        adv.RemainingAmount -= payThis;
                        adv.Status = adv.RemainingAmount <= 0 ? "مسددة بالكامل" : "سداد جزئي";
                        remDeduct -= payThis;

                        await _advanceRepository.UpdateAsync(adv);
                        await _advancePaymentRepository.AddAsync(new EmployeeAdvancePayment
                        {
                            EmployeeAdvanceId = adv.Id,
                            PaymentDate = DateTime.Today,
                            AmountPaid = payThis,
                            Notes = $"خصم تلقائي من راتب شهر {SalaryMonth}/{SalaryYear}"
                        });
                    }
                }

                IsSalaryPaymentDialogOpen = false;
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== 5. ATTENDANCE REGISTRATION ====================
        [RelayCommand]
        private void OpenAttendanceDialog()
        {
            SelectedAttendanceEmployee = Employees.FirstOrDefault();
            AttendanceDate = DateTime.Today;
            CheckInTime = new TimeSpan(9, 0, 0);
            CheckOutTime = new TimeSpan(17, 0, 0);
            AttendanceStatus = "حاضر";
            AttendanceNotes = string.Empty;
            IsAttendanceDialogOpen = true;
        }

        [RelayCommand]
        private void CloseAttendanceDialog() => IsAttendanceDialogOpen = false;

        [RelayCommand]
        private async Task SaveAttendanceAsync()
        {
            if (SelectedAttendanceEmployee == null) return;

            IsBusy = true;
            try
            {
                double hrs = (CheckOutTime > CheckInTime) ? (CheckOutTime - CheckInTime).TotalHours : 0;
                var att = new EmployeeAttendance
                {
                    EmployeeId = SelectedAttendanceEmployee.Id,
                    Date = AttendanceDate,
                    CheckInTime = CheckInTime,
                    CheckOutTime = CheckOutTime,
                    Status = AttendanceStatus,
                    WorkHours = hrs,
                    Notes = AttendanceNotes
                };

                await _attendanceRepository.AddAsync(att);
                IsAttendanceDialogOpen = false;
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== 6. DEDUCTION REGISTRATION ====================
        [RelayCommand]
        private void OpenDeductionDialog()
        {
            SelectedDeductionEmployee = Employees.FirstOrDefault();
            DeductionDate = DateTime.Today;
            DeductionAmount = 0;
            DeductionReason = string.Empty;
            DeductionNotes = string.Empty;
            IsDeductionDialogOpen = true;
        }

        [RelayCommand]
        private void CloseDeductionDialog() => IsDeductionDialogOpen = false;

        [RelayCommand]
        private async Task SaveDeductionAsync()
        {
            if (SelectedDeductionEmployee == null || DeductionAmount <= 0)
            {
                Axon.UI.Views.AxonMessageBox.Show("يرجى اختيار الموظف وإدخال قيمة الخصم!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var ded = new EmployeeDeduction
                {
                    EmployeeId = SelectedDeductionEmployee.Id,
                    DeductionDate = DeductionDate,
                    Amount = DeductionAmount,
                    Reason = DeductionReason,
                    Notes = DeductionNotes
                };

                await _deductionRepository.AddAsync(ded);
                IsDeductionDialogOpen = false;
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== 7. LEAVE REGISTRATION ====================
        [RelayCommand]
        private void OpenLeaveDialog()
        {
            SelectedLeaveEmployee = Employees.FirstOrDefault();
            LeaveStartDate = DateTime.Today;
            LeaveEndDate = DateTime.Today;
            LeaveType = "إجازة إعتيادية";
            LeaveReason = string.Empty;
            IsLeaveDialogOpen = true;
        }

        [RelayCommand]
        private void CloseLeaveDialog() => IsLeaveDialogOpen = false;

        [RelayCommand]
        private async Task SaveLeaveAsync()
        {
            if (SelectedLeaveEmployee == null) return;
            if (LeaveEndDate < LeaveStartDate)
            {
                Axon.UI.Views.AxonMessageBox.Show("تاريخ النهاية يجب أن يكون بعد تاريخ البداية!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                int days = (LeaveEndDate - LeaveStartDate).Days + 1;
                var lev = new EmployeeLeave
                {
                    EmployeeId = SelectedLeaveEmployee.Id,
                    StartDate = LeaveStartDate,
                    EndDate = LeaveEndDate,
                    LeaveType = LeaveType,
                    TotalDays = days,
                    Reason = LeaveReason,
                    Status = "مقبولة"
                };

                await _leaveRepository.AddAsync(lev);
                IsLeaveDialogOpen = false;
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
