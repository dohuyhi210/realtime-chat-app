// ==============================================
// FILE: DTOs/LoginRequest.cs
// Mô tả: DTO cho request đăng nhập
// ==============================================
using System.ComponentModel.DataAnnotations;

namespace ChatServer.DTOs.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}