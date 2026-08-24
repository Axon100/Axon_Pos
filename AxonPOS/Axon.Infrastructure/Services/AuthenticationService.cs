using System;
using System.Threading.Tasks;
using Axon.Application.DTOs.Authentication;
using Axon.Application.Interfaces.Services;
using Axon.Domain.Entities;
using Axon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Axon.Infrastructure.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AxonDbContext _context;

        public AuthenticationService(AxonDbContext context)
        {
            _context = context;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var reqUsername = request.Username?.Trim() ?? string.Empty;
            var reqPassword = request.Password ?? string.Empty;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == reqUsername.ToLower());
            
            // Auto-heal: If database has no admin user, seed admin / admin123
            if (user == null && reqUsername.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == 1 || r.Name == "Administrator");
                if (adminRole == null)
                {
                    adminRole = new Role { Name = "Administrator", Description = "System Administrator" };
                    _context.Roles.Add(adminRole);
                    await _context.SaveChangesAsync();
                }

                user = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    RoleId = adminRole.Id,
                    IsActive = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            if (user == null || !user.IsActive)
            {
                return new LoginResponseDto { Success = false, ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة." };
            }

            // Always unlock Admin account automatically to prevent admin lockout
            if (user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
            }
            else if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                return new LoginResponseDto { Success = false, ErrorMessage = "الحساب مقفل مؤقتاً بكثرة محاولات الدخول الخاطئة." };
            }

            // Verify Password (supports both BCrypt hash and fallback plaintext)
            bool isValid = false;
            if (user.PasswordHash == reqPassword)
            {
                isValid = true;
                // Upgrade hash to BCrypt
                try
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(reqPassword);
                }
                catch { }
            }
            else
            {
                try
                {
                    isValid = BCrypt.Net.BCrypt.Verify(reqPassword, user.PasswordHash);
                }
                catch
                {
                    // Fallback comparison
                    isValid = (user.PasswordHash == reqPassword);
                }
            }

            if (!isValid)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
                }
                await _context.SaveChangesAsync();
                
                return new LoginResponseDto { Success = false, ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة." };
            }

            // Success
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTimeOffset.UtcNow;
            
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = true,
                UserId = user.Id,
                RoleId = user.RoleId,
                Username = user.Username
            };
        }

        public async Task LogoutAsync(int userId)
        {
            await Task.CompletedTask;
        }

        public async Task<(bool Success, string ErrorMessage)> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "المستخدم غير موجود في النظام.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
                return (false, "كلمة المرور الجديدة قصيرة جداً (4 أحرف على الأقل).");

            // Verify old password (BCrypt or plaintext fallback)
            bool isValid;
            try { isValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash); }
            catch { isValid = (user.PasswordHash == oldPassword); }

            if (!isValid)
                return (false, "كلمة المرور القديمة غير صحيحة.");

            // Hash and save new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return (true, "✓ تم تغيير كلمة المرور بنجاح.");
        }
    }
}
