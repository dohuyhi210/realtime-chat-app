// ==============================================
// FILE: DTOs/MarkReadRequest.cs
// Mô tả: DTO cho request đánh dấu đã đọc
// ==============================================
namespace ChatServer.DTOs.Message
{
    public class MarkReadRequest
    {
        public int SenderId { get; set; }  // Đánh dấu đã đọc tất cả tin nhắn từ user này
    }
}