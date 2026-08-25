using Axon.UI.Helpers;
using Axon.UI.Services;
using Axon.UI.ViewModels.Base;
using Axon.Application.Interfaces.Services;
using Axon.Application.Interfaces.Repositories;
using Axon.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using Axon.UI.Views;

namespace Axon.UI.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly IBackupService _backupService;
        private readonly IRepository<SystemSetting> _systemSettingRepository;
        private readonly IDatabaseConfigService _databaseConfigService;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IPermissionService _permissionService;
        private readonly IAuthenticationService _authenticationService;

        // ─── Change Password Properties ────────────────────────────────────────
        [ObservableProperty] private string _oldPassword = string.Empty;
        [ObservableProperty] private string _newPassword = string.Empty;
        [ObservableProperty] private string _confirmPassword = string.Empty;
        [ObservableProperty] private string _changePasswordMessage = string.Empty;
        [ObservableProperty] private bool _isChangePasswordSuccess;
        [ObservableProperty] private bool _isChangePasswordMessageVisible;
        [ObservableProperty]
        private string _legalStoreName = AppResources.GetString("DefaultStoreName", "Axon POS Store");

        [ObservableProperty]
        private string _displayName = AppResources.GetString("DefaultTerminalName", "Axon POS Terminal 01");

        [ObservableProperty]
        private string _registrationNumber = "REG-00000-AXON";

        [ObservableProperty]
        private string _primaryAddress = "";

        [ObservableProperty]
        private string _city = "";

        [ObservableProperty]
        private string _state = "";

        [ObservableProperty]
        private string _postal = "";

        [ObservableProperty]
        private string _supportEmail = "support@axonpos.com";

        [ObservableProperty]
        private string _supportPhone = "";

        [ObservableProperty]
        private bool _isAcceptingOrders = true;

        // Database Configuration Properties
        [ObservableProperty]
        private string _serverName = ".\\SQLEXPRESS";

        [ObservableProperty]
        private string _databaseName = "AxonPOS";

        [ObservableProperty]
        private bool _useWindowsAuth = true;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isAdvancedMode;

        [ObservableProperty]
        private string _fullConnectionString = string.Empty;

        [ObservableProperty]
        private string _dbStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isDbSuccessStatus;

        [ObservableProperty]
        private bool _isDbStatusVisible;

        // User Management Properties
        [ObservableProperty]
        private bool _isAddUserDialogOpen;

        [ObservableProperty]
        private string _newUsername = string.Empty;

        [ObservableProperty]
        private string _newUserPassword = string.Empty;

        [ObservableProperty]
        private RoleModel? _selectedRole;

        public ObservableCollection<UserModel> Users { get; } = new();
        public ObservableCollection<RoleModel> Roles { get; } = new();

        // RBAC Permission Management Properties
        [ObservableProperty]
        private UserModel? _selectedUserForPermissions;

        [ObservableProperty]
        private RoleModel? _selectedRoleForPermissions;

        [ObservableProperty]
        private string _newCustomRoleName = string.Empty;

        [ObservableProperty]
        private bool _isAddRoleDialogOpen;

        [ObservableProperty]
        private bool _isManagingRolePermissions = true; // true = Role permissions, false = User-specific overrides
        private readonly System.Threading.SemaphoreSlim _dbLock = new(1, 1);
        private bool _isInitializing = false;

        partial void OnSelectedRoleForPermissionsChanged(RoleModel? value)
        {
            if (value != null && IsManagingRolePermissions && !_isInitializing)
            {
                _ = LoadRolePermissionsAsync(value.Id);
            }
        }

        partial void OnSelectedUserForPermissionsChanged(UserModel? value)
        {
            if (value != null && !IsManagingRolePermissions && !_isInitializing)
            {
                _ = LoadUserPermissionsAsync(value.Id);
            }
        }

        partial void OnIsManagingRolePermissionsChanged(bool value)
        {
            if (_isInitializing) return;
            if (value && SelectedRoleForPermissions != null)
            {
                _ = LoadRolePermissionsAsync(SelectedRoleForPermissions.Id);
            }
            else if (!value && SelectedUserForPermissions != null)
            {
                _ = LoadUserPermissionsAsync(SelectedUserForPermissions.Id);
            }
        }

        [ObservableProperty]
        private string _rbacStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isRbacStatusVisible;

        public ObservableCollection<PermissionModuleGroupModel> PermissionModuleGroups { get; } = new();

        public SettingsViewModel(
            IBackupService backupService, 
            IRepository<SystemSetting> systemSettingRepository, 
            IDatabaseConfigService databaseConfigService,
            IRepository<User> userRepository,
            IRepository<Role> roleRepository,
            IPermissionService permissionService,
            IAuthenticationService authenticationService)
        {
            _backupService = backupService;
            _systemSettingRepository = systemSettingRepository;
            _databaseConfigService = databaseConfigService;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _permissionService = permissionService;
            _authenticationService = authenticationService;
            Title = AppResources.GetString("Settings", "System Settings");

            LoadDatabaseConfig();
            _ = LoadAllDataSequentiallyAsync();
        }

        private async Task LoadAllDataSequentiallyAsync()
        {
            _isInitializing = true;
            try
            {
                await LoadSettingsAsync();
                await LoadUsersAndRolesAsync();
                await InitializeRbacAsync();
            }
            finally
            {
                _isInitializing = false;
            }
        }

        // ─── Change Password Command ───────────────────────────────────────────
        [RelayCommand]
        private async Task ChangePasswordAsync()
        {
            // Reset message
            IsChangePasswordMessageVisible = false;

            if (string.IsNullOrWhiteSpace(OldPassword))
            {
                ChangePasswordMessage = "يرجى إدخال كلمة المرور القديمة.";
                IsChangePasswordSuccess = false;
                IsChangePasswordMessageVisible = true;
                return;
            }
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ChangePasswordMessage = "يرجى إدخال كلمة المرور الجديدة.";
                IsChangePasswordSuccess = false;
                IsChangePasswordMessageVisible = true;
                return;
            }
            if (NewPassword != ConfirmPassword)
            {
                ChangePasswordMessage = "كلمة المرور الجديدة وتأكيدها غير متطابقين.";
                IsChangePasswordSuccess = false;
                IsChangePasswordMessageVisible = true;
                return;
            }

            IsBusy = true;
            try
            {
                var (success, message) = await _authenticationService.ChangePasswordAsync(
                    UserSessionService.CurrentUserId, OldPassword, NewPassword);

                ChangePasswordMessage = message;
                IsChangePasswordSuccess = success;
                IsChangePasswordMessageVisible = true;

                if (success)
                {
                    OldPassword = string.Empty;
                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ChangePasswordMessage = $"حدث خطأ: {ex.Message}";
                IsChangePasswordSuccess = false;
                IsChangePasswordMessageVisible = true;
            }
            finally { IsBusy = false; }
        }

        private async Task LoadUsersAndRolesAsync()
        {
            try
            {
                Roles.Clear();
                var dbRoles = await _roleRepository.GetAllAsync();
                if (dbRoles.Count == 0)
                {
                    var adminRole = await _roleRepository.AddAsync(new Role { Name = "مدير النظام (Admin)", Description = "صلاحيات كاملة للتحكم بالنظام" });
                    var cashierRole = await _roleRepository.AddAsync(new Role { Name = "كاشير (Cashier)", Description = "صلاحيات نقطة البيع والمبيعات فقط" });
                    var managerRole = await _roleRepository.AddAsync(new Role { Name = "مدير مخزن (Inventory Manager)", Description = "صلاحيات إدارة المنتجات والمخزون" });
                    dbRoles = new[] { adminRole, cashierRole, managerRole };
                }

                foreach (var r in dbRoles)
                {
                    Roles.Add(new RoleModel { Id = r.Id, Name = r.Name });
                }
                SelectedRole = Roles.FirstOrDefault();

                Users.Clear();
                var dbUsers = await _userRepository.GetAllAsync();
                foreach (var u in dbUsers)
                {
                    var roleName = dbRoles.FirstOrDefault(r => r.Id == u.RoleId)?.Name ?? "عام";
                    Users.Add(new UserModel
                    {
                        Id = u.Id,
                        Username = u.Username,
                        RoleName = roleName,
                        IsActive = u.IsActive
                    });
                }
                SelectedRoleForPermissions = Roles.FirstOrDefault();
                SelectedUserForPermissions = Users.FirstOrDefault();
            }
            catch
            {
                // Soft fallback if DB uninitialized
            }
        }

        [RelayCommand]
        private void OpenAddUserDialog()
        {
            NewUsername = string.Empty;
            NewUserPassword = string.Empty;
            SelectedRole = Roles.FirstOrDefault();
            IsAddUserDialogOpen = true;
        }

        [RelayCommand]
        private void CloseAddUserDialog()
        {
            IsAddUserDialogOpen = false;
        }

        [RelayCommand]
        private async Task SaveUserAsync()
        {
            if (string.IsNullOrWhiteSpace(NewUsername) || SelectedRole == null) return;

            IsBusy = true;
            try
            {
                var newUser = new User
                {
                    Username = NewUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(string.IsNullOrWhiteSpace(NewUserPassword) ? "123456" : NewUserPassword),
                    RoleId = SelectedRole.Id,
                    IsActive = true
                };

                await _userRepository.AddAsync(newUser);
                IsAddUserDialogOpen = false;
                await LoadUsersAndRolesAsync();
            }
            catch (System.Exception ex)
            {
                DbStatusMessage = $"فشل إضافة المستخدم: {ex.Message}";
                IsDbStatusVisible = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadDatabaseConfig()
        {
            try
            {
                var connStr = _databaseConfigService.GetConnectionString();
                if (!string.IsNullOrWhiteSpace(connStr))
                {
                    FullConnectionString = connStr;
                }
            }
            catch
            {
                // Fallback
            }
        }

        private string GetActiveConnectionString()
        {
            return _databaseConfigService.BuildConnectionString(
                ServerName,
                DatabaseName,
                UseWindowsAuth,
                Username,
                Password,
                IsAdvancedMode ? FullConnectionString : null
            );
        }

        [RelayCommand]
        private void SetLocalExpressSample()
        {
            IsAdvancedMode = true;
            FullConnectionString = "Data Source=.\\SQLEXPRESS;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=\"AxonPOS\";Command Timeout=0";
        }

        [RelayCommand]
        private void SetCloudDbSample()
        {
            IsAdvancedMode = true;
            FullConnectionString = "Data Source=db42178.public.databaseasp.net;Initial Catalog=db42178;Persist Security Info=False;User ID=db42178;Password=your_password;Pooling=False;MultipleActiveResultSets=True;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;Application Name=AxonPOS;Command Timeout=0";
        }

        [RelayCommand]
        private async Task TestDbConnectionAsync()
        {
            IsBusy = true;
            IsDbStatusVisible = false;
            try
            {
                var connStr = GetActiveConnectionString();
                var result = await _databaseConfigService.TestConnectionAsync(connStr);
                DbStatusMessage = result.Message;
                IsDbSuccessStatus = result.Success;
                IsDbStatusVisible = true;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AxonMessageBox.Show(
                        result.Message,
                        result.Success ? "اختبار الاتصال - نجاح" : "اختبار الاتصال - فشل",
                        System.Windows.MessageBoxButton.OK,
                        result.Success ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Warning
                    );
                });
            }
            catch (System.Exception ex)
            {
                DbStatusMessage = $"حدث خطأ غير متوقع أثناء اختبار الاتصال: {ex.Message}";
                IsDbSuccessStatus = false;
                IsDbStatusVisible = true;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AxonMessageBox.Show(
                        DbStatusMessage,
                        "خطأ في الاتصال",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error
                    );
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveDbConnectionAsync()
        {
            IsBusy = true;
            IsDbStatusVisible = false;
            try
            {
                var connStr = GetActiveConnectionString();
                _databaseConfigService.SaveConnectionString(connStr);
                DbStatusMessage = "تم حفظ إعدادات اتصال قاعدة البيانات بنجاح في ملف الإعدادات المحلي.";
                IsDbSuccessStatus = true;
                IsDbStatusVisible = true;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AxonMessageBox.Show(
                        DbStatusMessage,
                        "حفظ الإعدادات",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information
                    );
                });
            }
            catch (System.Exception ex)
            {
                DbStatusMessage = $"فشل الحفظ: {ex.Message}";
                IsDbSuccessStatus = false;
                IsDbStatusVisible = true;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AxonMessageBox.Show(
                        DbStatusMessage,
                        "خطأ في الحفظ",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error
                    );
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CreateAndMigrateDbAsync()
        {
            IsBusy = true;
            IsDbStatusVisible = false;
            try
            {
                var connStr = GetActiveConnectionString();
                var result = await _databaseConfigService.CreateAndMigrateDatabaseAsync(connStr);
                if (result.Success)
                {
                    _databaseConfigService.SaveConnectionString(connStr);
                }
                DbStatusMessage = result.Message;
                IsDbSuccessStatus = result.Success;
                IsDbStatusVisible = true;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AxonMessageBox.Show(
                        result.Message,
                        result.Success ? "إنشاء وترحيل قاعدة البيانات - نجاح" : "إنشاء وترحيل قاعدة البيانات - فشل",
                        System.Windows.MessageBoxButton.OK,
                        result.Success ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Error
                    );
                });
            }
            catch (System.Exception ex)
            {
                DbStatusMessage = $"حدث خطأ غير متوقع أثناء التكشيف والترحيل: {ex.Message}";
                IsDbSuccessStatus = false;
                IsDbStatusVisible = true;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AxonMessageBox.Show(
                        DbStatusMessage,
                        "خطأ في التكشيف والترحيل",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error
                    );
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSettingsAsync()
        {
            IsBusy = true;
            try
            {
                var settings = await _systemSettingRepository.GetAllAsync();
                var storeName = settings.FirstOrDefault(s => s.Key == "LegalStoreName");
                if (storeName != null && !string.IsNullOrEmpty(storeName.Value))
                {
                    LegalStoreName = storeName.Value;
                }
            }
            catch
            {
                // Soft fallback if DB is uninitialized
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            IsBusy = true;
            try
            {
                var settings = await _systemSettingRepository.GetAllAsync();
                var storeName = settings.FirstOrDefault(s => s.Key == "LegalStoreName");
                if (storeName == null)
                {
                    await _systemSettingRepository.AddAsync(new SystemSetting { Key = "LegalStoreName", Value = LegalStoreName });
                }
                else
                {
                    storeName.Value = LegalStoreName;
                    await _systemSettingRepository.UpdateAsync(storeName);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task InitializeRbacAsync()
        {
            try
            {
                await _permissionService.EnsureDefaultPermissionsSeededAsync();
                var allPermissions = await _permissionService.GetAllPermissionsAsync();

                PermissionModuleGroups.Clear();
                var grouped = allPermissions.GroupBy(p => p.Module);

                foreach (var group in grouped)
                {
                    var moduleGroup = new PermissionModuleGroupModel { ModuleName = group.Key };
                    foreach (var p in group)
                    {
                        moduleGroup.Permissions.Add(new PermissionItemModel
                        {
                            Code = p.Code,
                            Name = p.Name,
                            Page = p.Page,
                            Action = p.Action,
                            Description = p.Description,
                            IsGranted = false
                        });
                    }
                    PermissionModuleGroups.Add(moduleGroup);
                }

                SelectedRoleForPermissions = Roles.FirstOrDefault();
                if (SelectedRoleForPermissions != null)
                {
                    await LoadRolePermissionsAsync(SelectedRoleForPermissions.Id);
                }
            }
            catch (System.Exception ex)
            {
                RbacStatusMessage = $"خطأ في تحميل شجرة الصلاحيات: {ex.Message}";
                IsRbacStatusVisible = true;
            }
        }

        [RelayCommand]
        private void OpenAddRoleDialog()
        {
            NewCustomRoleName = string.Empty;
            IsAddRoleDialogOpen = true;
        }

        [RelayCommand]
        private void CloseAddRoleDialog()
        {
            IsAddRoleDialogOpen = false;
        }

        [RelayCommand]
        private async Task SaveNewRoleAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCustomRoleName)) return;

            IsBusy = true;
            try
            {
                var role = new Role
                {
                    Name = NewCustomRoleName.Trim(),
                    Description = "رتبة مخصصة محددة الصلاحيات"
                };

                var createdRole = await _roleRepository.AddAsync(role);
                var roleModel = new RoleModel { Id = createdRole.Id, Name = createdRole.Name };
                Roles.Add(roleModel);
                SelectedRoleForPermissions = roleModel;
                IsAddRoleDialogOpen = false;

                // Deselect all by default for fresh custom role
                foreach (var g in PermissionModuleGroups)
                {
                    foreach (var p in g.Permissions)
                    {
                        p.IsGranted = false;
                    }
                }

                RbacStatusMessage = $"تم إنشاء الرتبة ({createdRole.Name}) بنجاح! حدد الصلاحيات المطلوبة بالأسفل ثم اضغط 'حفظ وتطبيق الصلاحيات'.";
                IsRbacStatusVisible = true;
            }
            catch (Exception ex)
            {
                RbacStatusMessage = $"فشل إنشاء الرتبة: {ex.Message}";
                IsRbacStatusVisible = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SelectRoleForPermissionsAsync(RoleModel? role)
        {
            if (role == null) return;
            SelectedRoleForPermissions = role;
            IsManagingRolePermissions = true;
            await LoadRolePermissionsAsync(role.Id);
        }

        [RelayCommand]
        private async Task SelectUserForPermissionsAsync(UserModel? user)
        {
            if (user == null) return;
            SelectedUserForPermissions = user;
            IsManagingRolePermissions = false;
            await LoadUserPermissionsAsync(user.Id);
        }

        private async Task LoadRolePermissionsAsync(int roleId)
        {
            await _dbLock.WaitAsync();
            try
            {
                var grantedCodes = (await _permissionService.GetRolePermissionCodesAsync(roleId)).ToHashSet(System.StringComparer.OrdinalIgnoreCase);

                foreach (var group in PermissionModuleGroups)
                {
                    foreach (var perm in group.Permissions)
                    {
                        perm.IsGranted = grantedCodes.Contains(perm.Code);
                    }
                }
            }
            catch (Exception ex)
            {
                RbacStatusMessage = $"خطأ في تحميل صلاحيات الرتبة: {ex.Message}";
                IsRbacStatusVisible = true;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        private async Task LoadUserPermissionsAsync(int userId)
        {
            await _dbLock.WaitAsync();
            try
            {
                var effectiveCodes = await _permissionService.GetUserEffectivePermissionsAsync(userId);

                foreach (var group in PermissionModuleGroups)
                {
                    foreach (var perm in group.Permissions)
                    {
                        perm.IsGranted = effectiveCodes.Contains(perm.Code);
                    }
                }
            }
            catch (Exception ex)
            {
                RbacStatusMessage = $"خطأ في تحميل صلاحيات المستخدم: {ex.Message}";
                IsRbacStatusVisible = true;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        [RelayCommand]
        private void SelectAllPermissions()
        {
            foreach (var group in PermissionModuleGroups)
            {
                foreach (var perm in group.Permissions)
                {
                    perm.IsGranted = true;
                }
            }
        }

        [RelayCommand]
        private void DeselectAllPermissions()
        {
            foreach (var group in PermissionModuleGroups)
            {
                foreach (var perm in group.Permissions)
                {
                    perm.IsGranted = false;
                }
            }
        }

        [RelayCommand]
        private async Task SaveRbacPermissionsAsync()
        {
            IsBusy = true;
            IsRbacStatusVisible = false;
            try
            {
                var selectedCodes = PermissionModuleGroups
                    .SelectMany(g => g.Permissions)
                    .Where(p => p.IsGranted)
                    .Select(p => p.Code)
                    .ToList();

                if (IsManagingRolePermissions)
                {
                    if (SelectedRoleForPermissions == null) return;
                    await _permissionService.SaveRolePermissionsAsync(SelectedRoleForPermissions.Id, selectedCodes);
                    RbacStatusMessage = $"تم حفظ صلاحيات الرتبة ({SelectedRoleForPermissions.Name}) بنجاح وقيد التطبيق اللحظي!";
                }
                else
                {
                    if (SelectedUserForPermissions == null) return;
                    
                    var overrides = new System.Collections.Generic.Dictionary<string, bool>();
                    foreach (var p in PermissionModuleGroups.SelectMany(g => g.Permissions))
                    {
                        overrides[p.Code] = p.IsGranted;
                    }

                    await _permissionService.SaveUserPermissionsAsync(SelectedUserForPermissions.Id, overrides);
                    RbacStatusMessage = $"تم حفظ الصلاحيات المخصصة للمستخدم ({SelectedUserForPermissions.Username}) بنجاح!";
                }

                // If updating current active user session, refresh UserSessionService & MainWindowViewModel
                if ((!IsManagingRolePermissions && SelectedUserForPermissions?.Id == UserSessionService.CurrentUserId) ||
                    (IsManagingRolePermissions && SelectedRoleForPermissions?.Id == UserSessionService.CurrentRoleId))
                {
                    var updatedPerms = await _permissionService.GetUserEffectivePermissionsAsync(UserSessionService.CurrentUserId);
                    UserSessionService.SetPermissions(updatedPerms);
                }

                IsRbacStatusVisible = true;
            }
            catch (System.Exception ex)
            {
                RbacStatusMessage = $"فشل حفظ الصلاحيات: {ex.Message}";
                IsRbacStatusVisible = true;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public partial class PermissionItemModel : ObservableObject
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Page { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isGranted;
    }

    public class PermissionModuleGroupModel
    {
        public string ModuleName { get; set; } = string.Empty;
        public ObservableCollection<PermissionItemModel> Permissions { get; set; } = new();
    }

    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class RoleModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
