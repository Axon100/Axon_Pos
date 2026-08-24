namespace Axon.Application.DTOs.Authentication
{
    public class LoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool KeepMeSignedIn { get; set; }
    }

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}
