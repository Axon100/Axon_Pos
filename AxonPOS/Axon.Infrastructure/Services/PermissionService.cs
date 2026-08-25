using Axon.Application.Interfaces.Services;
using Axon.Domain.Entities;
using Axon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Axon.Infrastructure.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AxonDbContext _dbContext;

        public PermissionService(AxonDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task EnsureDefaultPermissionsSeededAsync()
        {
            // Execute automatic schema patch for existing databases missing new RBAC columns or audit fields
            try
            {
                if (_dbContext.Database.IsSqlServer())
                {
                    await _dbContext.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Permissions]') AND name = 'Code')
                        BEGIN
                            ALTER TABLE [Permissions] ADD [Code] NVARCHAR(MAX) NOT NULL DEFAULT '';
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Permissions]') AND name = 'Page')
                        BEGIN
                            ALTER TABLE [Permissions] ADD [Page] NVARCHAR(MAX) NOT NULL DEFAULT '';
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Permissions]') AND name = 'Description')
                        BEGIN
                            ALTER TABLE [Permissions] ADD [Description] NVARCHAR(MAX) NOT NULL DEFAULT '';
                        END
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserPermissions')
                        BEGIN
                            CREATE TABLE [UserPermissions] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [UserId] INT NOT NULL,
                                [PermissionId] INT NOT NULL,
                                [IsGranted] BIT NOT NULL DEFAULT 1,
                                [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [CreatedBy] INT NULL,
                                [UpdatedAt] DATETIMEOFFSET NULL,
                                [UpdatedBy] INT NULL,
                                [IsDeleted] BIT NOT NULL DEFAULT 0,
                                [DeletedAt] DATETIMEOFFSET NULL,
                                [DeletedBy] INT NULL,
                                [RowVersion] VARBINARY(8) NULL,
                                CONSTRAINT [FK_UserPermissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
                                CONSTRAINT [FK_UserPermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE
                            );
                        END
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Employees')
                        BEGIN
                            CREATE TABLE [Employees] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [FullName] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [JobTitle] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [Phone] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [NationalId] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [BasicSalary] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [HireDate] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [IsActive] BIT NOT NULL DEFAULT 1,
                                [UserId] INT NULL,
                                [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [CreatedBy] INT NULL,
                                [UpdatedAt] DATETIMEOFFSET NULL,
                                [UpdatedBy] INT NULL,
                                [IsDeleted] BIT NOT NULL DEFAULT 0,
                                [DeletedAt] DATETIMEOFFSET NULL,
                                [DeletedBy] INT NULL,
                                [RowVersion] VARBINARY(8) NULL
                            );
                        END
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmployeeAdvances')
                        BEGIN
                            CREATE TABLE [EmployeeAdvances] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [EmployeeId] INT NOT NULL,
                                [Amount] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [PaidAmount] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [RemainingAmount] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [AdvanceDate] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [Notes] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [Status] NVARCHAR(MAX) NOT NULL DEFAULT 'غير مسددة',
                                [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [CreatedBy] INT NULL,
                                [UpdatedAt] DATETIMEOFFSET NULL,
                                [UpdatedBy] INT NULL,
                                [IsDeleted] BIT NOT NULL DEFAULT 0,
                                [DeletedAt] DATETIMEOFFSET NULL,
                                [DeletedBy] INT NULL,
                                [RowVersion] VARBINARY(8) NULL
                            );
                        END
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmployeeAdvancePayments')
                        BEGIN
                            CREATE TABLE [EmployeeAdvancePayments] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [EmployeeAdvanceId] INT NOT NULL,
                                [PaymentDate] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [AmountPaid] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [Notes] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [CreatedBy] INT NULL,
                                [UpdatedAt] DATETIMEOFFSET NULL,
                                [UpdatedBy] INT NULL,
                                [IsDeleted] BIT NOT NULL DEFAULT 0,
                                [DeletedAt] DATETIMEOFFSET NULL,
                                [DeletedBy] INT NULL,
                                [RowVersion] VARBINARY(8) NULL
                            );
                        END
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmployeeSalaryPayments')
                        BEGIN
                            CREATE TABLE [EmployeeSalaryPayments] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [EmployeeId] INT NOT NULL,
                                [PaymentDate] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [Month] INT NOT NULL DEFAULT 1,
                                [Year] INT NOT NULL DEFAULT 2026,
                                [BasicSalary] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [BonusAmount] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [DeductionAmount] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [AdvanceDeduction] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [NetSalary] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [Notes] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [CreatedBy] INT NULL,
                                [UpdatedAt] DATETIMEOFFSET NULL,
                                [UpdatedBy] INT NULL,
                                [IsDeleted] BIT NOT NULL DEFAULT 0,
                                [DeletedAt] DATETIMEOFFSET NULL,
                                [DeletedBy] INT NULL,
                                [RowVersion] VARBINARY(8) NULL
                            );
                        END
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmployeeAttendances')
                        BEGIN
                            CREATE TABLE [EmployeeAttendances] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [EmployeeId] INT NOT NULL,
                                [Date] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [CheckInTime] TIME NULL,
                                [CheckOutTime] TIME NULL,
                                [Status] NVARCHAR(MAX) NOT NULL DEFAULT 'حاضر',
                                [WorkHours] FLOAT NOT NULL DEFAULT 0,
                                [Notes] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [CreatedBy] INT NULL,
                                [UpdatedAt] DATETIMEOFFSET NULL,
                                [UpdatedBy] INT NULL,
                                [IsDeleted] BIT NOT NULL DEFAULT 0,
                                [DeletedAt] DATETIMEOFFSET NULL,
                                [DeletedBy] INT NULL,
                                [RowVersion] VARBINARY(8) NULL
                            );
                        END
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmployeeDeductions')
                        BEGIN
                            CREATE TABLE [EmployeeDeductions] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [EmployeeId] INT NOT NULL,
                                [DeductionDate] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [Amount] DECIMAL(18,4) NOT NULL DEFAULT 0,
                                [Reason] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [Notes] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [CreatedBy] INT NULL,
                                [UpdatedAt] DATETIMEOFFSET NULL,
                                [UpdatedBy] INT NULL,
                                [IsDeleted] BIT NOT NULL DEFAULT 0,
                                [DeletedAt] DATETIMEOFFSET NULL,
                                [DeletedBy] INT NULL,
                                [RowVersion] VARBINARY(8) NULL
                            );
                        END
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmployeeLeaves')
                        BEGIN
                            CREATE TABLE [EmployeeLeaves] (
                                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [EmployeeId] INT NOT NULL,
                                [StartDate] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [EndDate] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [LeaveType] NVARCHAR(MAX) NOT NULL DEFAULT 'إجازة إعتيادية',
                                [TotalDays] INT NOT NULL DEFAULT 1,
                                [Reason] NVARCHAR(MAX) NOT NULL DEFAULT '',
                                [Status] NVARCHAR(MAX) NOT NULL DEFAULT 'مقبولة',
                                [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                                [CreatedBy] INT NULL,
                                [UpdatedAt] DATETIMEOFFSET NULL,
                                [UpdatedBy] INT NULL,
                                [IsDeleted] BIT NOT NULL DEFAULT 0,
                                [DeletedAt] DATETIMEOFFSET NULL,
                                [DeletedBy] INT NULL,
                                [RowVersion] VARBINARY(8) NULL
                            );
                        END
                    ");

                    var allTables = new[] { "Users", "Roles", "Permissions", "UserPermissions", "Products", "Categories", "Expenses", "Invoices", "Returns", "SaleLineItems", "ReturnLineItems", "InventoryTransactions", "StockMovements", "Payments", "SystemSettings", "AuditLogs", "Sales", "Employees", "EmployeeAdvances", "EmployeeAdvancePayments", "EmployeeSalaryPayments", "EmployeeAttendances", "EmployeeDeductions", "EmployeeLeaves" };
                    foreach (var tbl in allTables)
                    {
                        await _dbContext.Database.ExecuteSqlRawAsync($@"
                            IF EXISTS (SELECT * FROM sys.tables WHERE name = '{tbl}')
                            BEGIN
                                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[{tbl}]') AND name = 'CreatedBy')
                                    ALTER TABLE [{tbl}] ADD [CreatedBy] INT NULL;
                                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[{tbl}]') AND name = 'UpdatedBy')
                                    ALTER TABLE [{tbl}] ADD [UpdatedBy] INT NULL;
                                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[{tbl}]') AND name = 'DeletedBy')
                                    ALTER TABLE [{tbl}] ADD [DeletedBy] INT NULL;
                                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[{tbl}]') AND name = 'RowVersion')
                                    ALTER TABLE [{tbl}] ADD [RowVersion] VARBINARY(8) NULL;
                            END
                        ");
                    }
                }
                else if (_dbContext.Database.IsSqlite())
                {
                    try { await _dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Permissions ADD COLUMN Code TEXT NOT NULL DEFAULT '';"); } catch {}
                    try { await _dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Permissions ADD COLUMN Page TEXT NOT NULL DEFAULT '';"); } catch {}
                    try { await _dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Permissions ADD COLUMN Description TEXT NOT NULL DEFAULT '';"); } catch {}
                    try
                    {
                        await _dbContext.Database.ExecuteSqlRawAsync(@"
                            CREATE TABLE IF NOT EXISTS UserPermissions (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                UserId INTEGER NOT NULL,
                                PermissionId INTEGER NOT NULL,
                                IsGranted INTEGER NOT NULL DEFAULT 1,
                                CreatedAt TEXT NOT NULL,
                                CreatedBy INTEGER NULL,
                                UpdatedAt TEXT NULL,
                                UpdatedBy INTEGER NULL,
                                IsDeleted INTEGER NOT NULL DEFAULT 0,
                                DeletedAt TEXT NULL,
                                DeletedBy INTEGER NULL,
                                RowVersion BLOB NULL,
                                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                                FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
                            );
                        ");
                    } catch {}

                    try
                    {
                        await _dbContext.Database.ExecuteSqlRawAsync(@"
                            CREATE TABLE IF NOT EXISTS Employees (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                FullName TEXT NOT NULL DEFAULT '',
                                JobTitle TEXT NOT NULL DEFAULT '',
                                Phone TEXT NOT NULL DEFAULT '',
                                NationalId TEXT NOT NULL DEFAULT '',
                                BasicSalary TEXT NOT NULL DEFAULT '0',
                                HireDate TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                IsActive INTEGER NOT NULL DEFAULT 1,
                                UserId INTEGER NULL,
                                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                CreatedBy INTEGER NULL,
                                UpdatedAt TEXT NULL,
                                UpdatedBy INTEGER NULL,
                                IsDeleted INTEGER NOT NULL DEFAULT 0,
                                DeletedAt TEXT NULL,
                                DeletedBy INTEGER NULL,
                                RowVersion BLOB NULL
                            );
                            CREATE TABLE IF NOT EXISTS EmployeeAdvances (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                EmployeeId INTEGER NOT NULL,
                                Amount TEXT NOT NULL DEFAULT '0',
                                PaidAmount TEXT NOT NULL DEFAULT '0',
                                RemainingAmount TEXT NOT NULL DEFAULT '0',
                                AdvanceDate TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                Notes TEXT NOT NULL DEFAULT '',
                                Status TEXT NOT NULL DEFAULT 'غير مسددة',
                                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                CreatedBy INTEGER NULL,
                                UpdatedAt TEXT NULL,
                                UpdatedBy INTEGER NULL,
                                IsDeleted INTEGER NOT NULL DEFAULT 0,
                                DeletedAt TEXT NULL,
                                DeletedBy INTEGER NULL,
                                RowVersion BLOB NULL
                            );
                            CREATE TABLE IF NOT EXISTS EmployeeAdvancePayments (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                EmployeeAdvanceId INTEGER NOT NULL,
                                PaymentDate TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                AmountPaid TEXT NOT NULL DEFAULT '0',
                                Notes TEXT NOT NULL DEFAULT '',
                                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                CreatedBy INTEGER NULL,
                                UpdatedAt TEXT NULL,
                                UpdatedBy INTEGER NULL,
                                IsDeleted INTEGER NOT NULL DEFAULT 0,
                                DeletedAt TEXT NULL,
                                DeletedBy INTEGER NULL,
                                RowVersion BLOB NULL
                            );
                            CREATE TABLE IF NOT EXISTS EmployeeSalaryPayments (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                EmployeeId INTEGER NOT NULL,
                                PaymentDate TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                Month INTEGER NOT NULL DEFAULT 1,
                                Year INTEGER NOT NULL DEFAULT 2026,
                                BasicSalary TEXT NOT NULL DEFAULT '0',
                                BonusAmount TEXT NOT NULL DEFAULT '0',
                                DeductionAmount TEXT NOT NULL DEFAULT '0',
                                AdvanceDeduction TEXT NOT NULL DEFAULT '0',
                                NetSalary TEXT NOT NULL DEFAULT '0',
                                Notes TEXT NOT NULL DEFAULT '',
                                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                CreatedBy INTEGER NULL,
                                UpdatedAt TEXT NULL,
                                UpdatedBy INTEGER NULL,
                                IsDeleted INTEGER NOT NULL DEFAULT 0,
                                DeletedAt TEXT NULL,
                                DeletedBy INTEGER NULL,
                                RowVersion BLOB NULL
                            );
                            CREATE TABLE IF NOT EXISTS EmployeeAttendances (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                EmployeeId INTEGER NOT NULL,
                                Date TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                CheckInTime TEXT NULL,
                                CheckOutTime TEXT NULL,
                                Status TEXT NOT NULL DEFAULT 'حاضر',
                                WorkHours REAL NOT NULL DEFAULT 0,
                                Notes TEXT NOT NULL DEFAULT '',
                                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                CreatedBy INTEGER NULL,
                                UpdatedAt TEXT NULL,
                                UpdatedBy INTEGER NULL,
                                IsDeleted INTEGER NOT NULL DEFAULT 0,
                                DeletedAt TEXT NULL,
                                DeletedBy INTEGER NULL,
                                RowVersion BLOB NULL
                            );
                            CREATE TABLE IF NOT EXISTS EmployeeDeductions (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                EmployeeId INTEGER NOT NULL,
                                DeductionDate TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                Amount TEXT NOT NULL DEFAULT '0',
                                Reason TEXT NOT NULL DEFAULT '',
                                Notes TEXT NOT NULL DEFAULT '',
                                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                CreatedBy INTEGER NULL,
                                UpdatedAt TEXT NULL,
                                UpdatedBy INTEGER NULL,
                                IsDeleted INTEGER NOT NULL DEFAULT 0,
                                DeletedAt TEXT NULL,
                                DeletedBy INTEGER NULL,
                                RowVersion BLOB NULL
                            );
                            CREATE TABLE IF NOT EXISTS EmployeeLeaves (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                EmployeeId INTEGER NOT NULL,
                                StartDate TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                EndDate TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                LeaveType TEXT NOT NULL DEFAULT 'إجازة إعتيادية',
                                TotalDays INTEGER NOT NULL DEFAULT 1,
                                Reason TEXT NOT NULL DEFAULT '',
                                Status TEXT NOT NULL DEFAULT 'مقبولة',
                                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                CreatedBy INTEGER NULL,
                                UpdatedAt TEXT NULL,
                                UpdatedBy INTEGER NULL,
                                IsDeleted INTEGER NOT NULL DEFAULT 0,
                                DeletedAt TEXT NULL,
                                DeletedBy INTEGER NULL,
                                RowVersion BLOB NULL
                            );
                        ");
                    } catch {}

                    var sqliteTables = new[] { "Users", "Roles", "Permissions", "UserPermissions", "Products", "Categories", "Expenses", "Invoices", "Returns", "SaleLineItems", "ReturnLineItems", "InventoryTransactions", "StockMovements", "Payments", "SystemSettings", "AuditLogs", "Sales", "Employees", "EmployeeAdvances", "EmployeeAdvancePayments", "EmployeeSalaryPayments", "EmployeeAttendances", "EmployeeDeductions", "EmployeeLeaves" };
                    foreach (var tbl in sqliteTables)
                    {
                        try { await _dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE {tbl} ADD COLUMN CreatedBy INTEGER NULL;"); } catch {}
                        try { await _dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE {tbl} ADD COLUMN UpdatedBy INTEGER NULL;"); } catch {}
                        try { await _dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE {tbl} ADD COLUMN DeletedBy INTEGER NULL;"); } catch {}
                        try { await _dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE {tbl} ADD COLUMN RowVersion BLOB NULL;"); } catch {}
                    }
                }
            }
            catch
            {
                // Schema patch fallback
            }

            // Seed Roles if missing
            var existingRoles = await _dbContext.Roles.Include(r => r.Permissions).ToListAsync();
            
            var adminRole = existingRoles.FirstOrDefault(r => r.Id == 1 || r.Name == "Administrator");
            if (adminRole == null)
            {
                adminRole = new Role { Name = "Administrator", Description = "مدير النظام الفعلي بجميع الصلاحيات" };
                _dbContext.Roles.Add(adminRole);
            }

            var cashierRole = existingRoles.FirstOrDefault(r => r.Id == 2 || r.Name == "Cashier");
            if (cashierRole == null)
            {
                cashierRole = new Role { Name = "Cashier", Description = "كاشير المبيعات ونقطة البيع" };
                _dbContext.Roles.Add(cashierRole);
            }

            var invRole = existingRoles.FirstOrDefault(r => r.Id == 3 || r.Name == "InventoryManager");
            if (invRole == null)
            {
                invRole = new Role { Name = "InventoryManager", Description = "مسؤول ومراقب المخزن" };
                _dbContext.Roles.Add(invRole);
            }

            var userRole = existingRoles.FirstOrDefault(r => r.Id == 4 || r.Name == "User");
            if (userRole == null)
            {
                userRole = new Role { Name = "User", Description = "مستخدم عادي ذو صلاحيات مخصصة" };
                _dbContext.Roles.Add(userRole);
            }

            await _dbContext.SaveChangesAsync();

            // Defined standard permissions catalog
            var standardPermissions = new List<(string Code, string Name, string Module, string Page, string Action, string Description)>
            {
                // Dashboard
                ("Dashboard.View", "عرض لوحة التحكم", "الرئيسية", "لوحة التحكم", "View", "عرض إحصائيات النظام الشاملة"),

                // Products
                ("Products.View", "عرض قائمة المنتجات", "المنتجات", "إدارة المنتجات", "View", "عرض جدول قائمة المنتجات"),
                ("Products.Add", "إضافة منتج جديد", "المنتجات", "إدارة المنتجات", "Add", "إضافة صنف جديد لقاعدة البيانات"),
                ("Products.Edit", "تعديل بيانات منتج", "المنتجات", "إدارة المنتجات", "Edit", "تحديث أسعار وصور وبيانات المنتجات"),
                ("Products.Delete", "حذف منتج", "المنتجات", "إدارة المنتجات", "Delete", "حذف منتج نهائياً"),
                ("Products.Export", "تصدير المنتجات", "المنتجات", "إدارة المنتجات", "Export", "تصدير ملفات Excel و PDF"),
                ("Products.Print", "طباعة باركود المنتجات", "المنتجات", "طباعة الباركود", "Print", "توليد وطباعة الملصقات والباركودات"),

                // Inventory
                ("Inventory.View", "عرض حركة المخزون", "المخزون", "إدارة المخزون", "View", "عرض أرصداء المستودع والأصناف"),
                ("Inventory.StockIn", "توريد وتغذية شحنات", "المخزون", "إدارة المخزون", "StockIn", "إضافة كميات واردة للمخزن"),
                ("Inventory.StockOut", "صرف مخزون", "المخزون", "إدارة المخزون", "StockOut", "إخراج وصرف كميات"),
                ("Inventory.Adjustment", "تسوية المخزون", "المخزون", "إدارة المخزون", "Adjustment", "تعديل وجرد الكميات"),
                ("Inventory.Delete", "حذف أصناف الجرد", "المخزون", "إدارة المخزون", "Delete", "حذف أصناف مخزنية"),

                // Sales / POS
                ("POS.View", "عرض شاشة البيع", "المبيعات", "نقطة البيع (POS)", "View", "فتح واجهة الكاشير المباشرة"),
                ("POS.Sell", "إتمام عملية بيع", "المبيعات", "نقطة البيع (POS)", "Sell", "طباعة فواتير وقبض مبيعات"),
                ("POS.Discount", "تطبيق خصم على الفاتورة", "المبيعات", "نقطة البيع (POS)", "Discount", "إضافة نسبة أو مبلغ خصم"),
                ("POS.Refund", "مرتجع وإلغاء فاتورة", "المبيعات", "نقطة البيع (POS)", "Refund", "إعادة البضاعة وإلغاء الفواتير"),

                // HR / Staff Management
                ("HR.View", "عرض شؤون العاملين", "شؤون العاملين", "شؤون العاملين", "View", "عرض إدارة الرواتب والحضور والغياب والخصومات"),
                ("HR.Edit", "تعديل وتسجيل بيانات الموظفين", "شؤون العاملين", "شؤون العاملين", "Edit", "تسجيل الرواتب والسلف وتسديدها والخصومات والإجازات"),
                ("HR.Reports", "عرض تقارير شؤون العاملين", "شؤون العاملين", "تقارير العاملين", "Reports", "عرض واستخراج تقارير الرواتب والحضور والخصومات"),

                // Expenses
                ("Expenses.View", "عرض قائمة المصروفات", "المصروفات", "إدارة المصروفات", "View", "عرض سجّلات السحبيات والمصروفات"),
                ("Expenses.Add", "تسجيل مصروف جديد", "المصروفات", "إدارة المصروفات", "Add", "إضافة سند صرف جديد"),
                ("Expenses.Edit", "تعديل سند مصروف", "المصروفات", "إدارة المصروفات", "Edit", "تعديل قيمة أو بيئة المصروف"),
                ("Expenses.Delete", "حذف سند مصروف", "المصروفات", "إدارة المصروفات", "Delete", "حذف سجل مصروف"),

                // Reports
                ("Reports.View", "عرض التقارير والأرباح", "التقارير", "الأرباح والتقارير", "View", "الاطلاع على تقارير الأرباح والمبيعات"),
                ("Reports.Export", "تصدير التقارير", "التقارير", "الأرباح والتقارير", "Export", "تصدير التقارير لـ PDF و Excel"),

                // Users & Security
                ("Users.View", "عرض قائمة المستخدمين", "المستخدمين", "إدارة المستخدمين", "View", "عرض جدول الحسابات بالكامل"),
                ("Users.Add", "إضافة مستخدم جديد", "المستخدمين", "إدارة المستخدمين", "Add", "إنشاء حسابات كاشير وموظفين"),
                ("Users.Edit", "تعديل بيانات مستخدم", "المستخدمين", "إدارة المستخدمين", "Edit", "تعديل الاسم كلمة السر والربط"),
                ("Users.Delete", "حذف حساب مستخدم", "المستخدمين", "إدارة المستخدمين", "Delete", "حذف موظف من النظام"),
                ("Users.Permissions", "تخصيص الصلاحيات", "المستخدمين", "إدارة الصلاحيات (RBAC)", "Permissions", "منح وسحب الصلاحيات للمستخدمين"),

                // Settings
                ("Settings.View", "عرض إعدادات النظام", "الإعدادات", "إعدادات النظام", "View", "عرض معلومات المؤسسة والنسخ"),
                ("Settings.Edit", "تعديل وحفظ الإعدادات", "الإعدادات", "إعدادات النظام", "Edit", "تغيير اسم المنشأة وقواعد البيانات")
            };

            var dbPermissions = await _dbContext.Permissions.ToListAsync();
            bool permissionsAdded = false;

            foreach (var perm in standardPermissions)
            {
                var existing = dbPermissions.FirstOrDefault(p => p.Code == perm.Code);
                if (existing == null)
                {
                    var newPerm = new Permission
                    {
                        Code = perm.Code,
                        Name = perm.Name,
                        Module = perm.Module,
                        Page = perm.Page,
                        Action = perm.Action,
                        Description = perm.Description
                    };
                    _dbContext.Permissions.Add(newPerm);
                    dbPermissions.Add(newPerm);
                    permissionsAdded = true;
                }
            }

            if (permissionsAdded)
            {
                await _dbContext.SaveChangesAsync();
            }

            // Assign ALL permissions to Administrator Role (Role #1)
            var allPerms = await _dbContext.Permissions.ToListAsync();
            adminRole = await _dbContext.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == "Administrator" || r.Id == 1);
            
            bool adminUpdated = false;
            if (adminRole != null)
            {
                foreach (var perm in allPerms)
                {
                    if (!adminRole.Permissions.Any(p => p.Id == perm.Id))
                    {
                        adminRole.Permissions.Add(perm);
                        adminUpdated = true;
                    }
                }
            }

            // Assign Cashier default permissions if empty
            var allRolesList = await _dbContext.Roles.Include(r => r.Permissions).ToListAsync();
            var cashierRoles = allRolesList
                .Where(r => r.Id == 2 || r.Name.Contains("Cashier", StringComparison.OrdinalIgnoreCase) || r.Name.Contains("كاشير"))
                .ToList();

            var cashierPermCodes = new[] { "Dashboard.View", "POS.View", "POS.Sell", "POS.Discount", "POS.Refund", "Products.View", "Inventory.View" };
            var cashierPermObjects = allPerms.Where(p => cashierPermCodes.Contains(p.Code)).ToList();

            foreach (var cRole in cashierRoles)
            {
                if (cRole != null && !cRole.Permissions.Any())
                {
                    foreach (var p in cashierPermObjects)
                    {
                        cRole.Permissions.Add(p);
                    }
                    adminUpdated = true;
                }
            }

            if (adminUpdated)
            {
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            return await _dbContext.Permissions.AsNoTracking().ToListAsync();
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _dbContext.Roles.Include(r => r.Permissions).AsNoTracking().ToListAsync();
        }

        public async Task<List<User>> GetAllUsersWithRolesAsync()
        {
            return await _dbContext.Users.Include(u => u.Role).Include(u => u.UserPermissions).ThenInclude(up => up.Permission).AsNoTracking().ToListAsync();
        }

        public async Task<HashSet<string>> GetUserEffectivePermissionsAsync(int userId)
        {
            var user = await _dbContext.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.Permissions)
                .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return new HashSet<string>();

            // Super Admin (Role 1 or Username admin) always has ALL permissions
            if (user.RoleId == 1 || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                var allCodes = await _dbContext.Permissions.Select(p => p.Code).ToListAsync();
                return new HashSet<string>(allCodes, StringComparer.OrdinalIgnoreCase);
            }

            var effectiveCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Load Role Default Permissions
            if (user.Role != null && user.Role.Permissions != null)
            {
                foreach (var p in user.Role.Permissions)
                {
                    if (!string.IsNullOrEmpty(p.Code))
                    {
                        effectiveCodes.Add(p.Code);
                    }
                }
            }

            // 2. Apply User Specific Overrides (Grant or Deny)
            if (user.UserPermissions != null)
            {
                foreach (var up in user.UserPermissions)
                {
                    if (up.Permission != null && !string.IsNullOrEmpty(up.Permission.Code))
                    {
                        if (up.IsGranted)
                        {
                            effectiveCodes.Add(up.Permission.Code);
                        }
                        else
                        {
                            effectiveCodes.Remove(up.Permission.Code);
                        }
                    }
                }
            }

            // Fallback for Cashier roles if permissions are empty
            if (effectiveCodes.Count == 0 && (user.RoleId == 2 || (user.Role != null && (user.Role.Name.Contains("Cashier", StringComparison.OrdinalIgnoreCase) || user.Role.Name.Contains("كاشير")))))
            {
                effectiveCodes.Add("POS.View");
                effectiveCodes.Add("POS.Sell");
                effectiveCodes.Add("POS.Discount");
                effectiveCodes.Add("POS.Refund");
                effectiveCodes.Add("Products.View");
                effectiveCodes.Add("Inventory.View");
            }

            return effectiveCodes;
        }

        public async Task<List<string>> GetRolePermissionCodesAsync(int roleId)
        {
            var role = await _dbContext.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null) return new List<string>();

            return role.Permissions.Select(p => p.Code).ToList();
        }

        public async Task<List<UserPermission>> GetUserSpecificPermissionsAsync(int userId)
        {
            return await _dbContext.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task SaveRolePermissionsAsync(int roleId, IEnumerable<string> permissionCodes)
        {
            var role = await _dbContext.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null) return;

            // Admin Role 1 cannot be stripped of permissions
            if (role.Id == 1) return;

            var targetCodes = permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var allPerms = await _dbContext.Permissions.ToListAsync();

            role.Permissions.Clear();
            foreach (var perm in allPerms)
            {
                if (targetCodes.Contains(perm.Code))
                {
                    role.Permissions.Add(perm);
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveUserPermissionsAsync(int userId, IDictionary<string, bool> permissionOverrides)
        {
            var user = await _dbContext.Users.Include(u => u.UserPermissions).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return;

            // Cannot strip Super Admin user 1
            if (user.Id == 1 || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase)) return;

            var existingOverrides = await _dbContext.UserPermissions.Where(up => up.UserId == userId).ToListAsync();
            _dbContext.UserPermissions.RemoveRange(existingOverrides);

            var allPerms = await _dbContext.Permissions.ToListAsync();

            foreach (var kvp in permissionOverrides)
            {
                var perm = allPerms.FirstOrDefault(p => p.Code.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                if (perm != null)
                {
                    _dbContext.UserPermissions.Add(new UserPermission
                    {
                        UserId = userId,
                        PermissionId = perm.Id,
                        IsGranted = kvp.Value
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<User> CreateUserAsync(string username, string password, int roleId)
        {
            var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (existing != null)
            {
                throw new InvalidOperationException($"اسم المستخدم ({username}) موجود بالفعل في النظام!");
            }

            var newUser = new User
            {
                Username = username,
                PasswordHash = password, // Will be used in authentication
                RoleId = roleId,
                IsActive = true
            };

            _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync();
            return newUser;
        }

        public async Task UpdateUserRoleAsync(int userId, int newRoleId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return;

            // Admin safety: Cannot change Admin user 1 role
            if (user.Id == 1 || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("لا يمكن تغيير رتبة حساب المدير العام الرئيسي (Admin)!");
            }

            user.RoleId = newRoleId;
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return;

            // Admin safety: Cannot delete super admin user 1
            if (user.Id == 1 || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("حماية النظام: لا يمكن حذف حساب المدير العام الرئيسي (Admin)!");
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
        }
    }
}
