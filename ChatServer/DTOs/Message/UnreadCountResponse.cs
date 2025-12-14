// ==============================================
// FILE: DTOs/UnreadCountResponse.cs
// Mô tả: DTO cho response số tin nhắn chưa đọc
// ==============================================
namespace ChatServer.DTOs.Message
{
    public class UnreadCountResponse
    {
        public bool Success { get; set; } = true;
        public int TotalUnread { get; set; }
        public Dictionary<int, int> UnreadByUser { get; set; } = new Dictionary<int, int>();
        // Key: SenderId, Value: Số tin nhắn chưa đọc từ user đó
    }
}