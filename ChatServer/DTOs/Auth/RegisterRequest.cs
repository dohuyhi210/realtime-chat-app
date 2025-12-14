// ==============================================
// FILE: DTOs/RegisterRequest.cs
// Mô tả: DTO cho request đăng ký
// ==============================================
using System.ComponentModel.DataAnnotations;

namespace ChatServer.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nickname is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Nickname must be between 2 and 100 characters")]
        public string Nickname { get; set; } = string.Empty;
    }
}