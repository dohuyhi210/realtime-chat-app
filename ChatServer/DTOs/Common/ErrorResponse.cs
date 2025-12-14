// ==============================================
// FILE: DTOs/ErrorResponse.cs
// Mô tả: DTO cho response lỗi
// ==============================================
namespace ChatServer.DTOs.Common
{
    public class ErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}