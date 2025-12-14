// ==============================================
// FILE: DTOs/AuthResponse.cs
// Mô tả: DTO cho response sau khi login/register thành công
// ==============================================
namespace ChatServer.DTOs.Auth
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
    }
}