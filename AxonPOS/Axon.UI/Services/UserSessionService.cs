using Axon.Application.DTOs.Authentication;
using System;
using System.Collections.Generic;

namespace Axon.UI.Services
{
    public static class UserSessionService
    {
        public static int CurrentUserId { get; set; } = 0;
        public static string CurrentUsername { get; set; } = string.Empty;
        public static int CurrentRoleId { get; set; } = 0; // 0 = None/Unauthenticated, 1 = Admin, 2 = Cashier, 3 = InventoryManager, 4 = User

        public static HashSet<string> ActivePermissions { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public static bool IsAuthenticated => CurrentUserId > 0;
        public static bool IsAdmin => IsAuthenticated && (CurrentRoleId == 1 || CurrentUsername.Equals("admin", StringComparison.OrdinalIgnoreCase));
        public static bool IsCashier => CurrentRoleId == 2;
        public static bool IsInventoryManager => CurrentRoleId == 3;
        public static bool IsUser => CurrentRoleId == 4;

        public static string RoleName => CurrentRoleId switch
        {
            1 => "المدير العام (Admin)",
            2 => "كاشير المبيعات (Cashier)",
            3 => "مسؤول المخزن (Inventory Manager)",
            4 => "مستخدم مخصص (User)",
            _ => "غير مسجل"
        };

        public static void SetSession(LoginResponseDto response, IEnumerable<string>? permissions = null)
        {
            CurrentUserId = response.UserId;
            CurrentUsername = response.Username;
            CurrentRoleId = response.RoleId;

            ActivePermissions.Clear();
            if (permissions != null)
            {
                foreach (var p in permissions)
                {
                    ActivePermissions.Add(p);
                }
            }
        }

        public static void SetPermissions(IEnumerable<string> permissions)
        {
            ActivePermissions.Clear();
            if (permissions != null)
            {
                foreach (var p in permissions)
                {
                    ActivePermissions.Add(p);
                }
            }
        }

        public static bool HasPermission(string permissionCode)
        {
            if (IsAdmin) return true; // Super Admin has all permissions bypass
            if (string.IsNullOrEmpty(permissionCode)) return true;

            return ActivePermissions.Contains(permissionCode);
        }

        public static void ClearSession()
        {
            CurrentUserId = 0;
            CurrentUsername = string.Empty;
            CurrentRoleId = 0;
            ActivePermissions.Clear();
        }
    }
}
