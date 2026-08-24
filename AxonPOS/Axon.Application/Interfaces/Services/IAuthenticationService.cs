using System.Threading.Tasks;
using Axon.Application.DTOs.Authentication;

namespace Axon.Application.Interfaces.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task LogoutAsync(int userId);
        Task<(bool Success, string ErrorMessage)> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    }
}
