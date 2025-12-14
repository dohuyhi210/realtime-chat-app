// ==============================================
// FILE: DTOs/SendMessageResponse.cs
// Mô tả: DTO cho response sau khi gửi tin nhắn
// ==============================================
namespace ChatServer.DTOs.Message
{
    public class SendMessageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public MessageDto? Data { get; set; }
    }
}