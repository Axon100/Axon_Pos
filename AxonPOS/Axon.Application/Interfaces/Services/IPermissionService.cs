using Axon.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Axon.Application.Interfaces.Services
{
    public interface IPermissionService
    {
        Task EnsureDefaultPermissionsSeededAsync();
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<List<Role>> GetAllRolesAsync();
        Task<List<User>> GetAllUsersWithRolesAsync();
        Task<HashSet<string>> GetUserEffectivePermissionsAsync(int userId);
        Task<List<string>> GetRolePermissionCodesAsync(int roleId);
        Task<List<UserPermission>> GetUserSpecificPermissionsAsync(int userId);
        Task SaveRolePermissionsAsync(int roleId, IEnumerable<string> permissionCodes);
        Task SaveUserPermissionsAsync(int userId, IDictionary<string, bool> permissionOverrides);
        Task<User> CreateUserAsync(string username, string password, int roleId);
        Task UpdateUserRoleAsync(int userId, int newRoleId);
        Task DeleteUserAsync(int userId);
    }
}
