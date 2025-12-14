// ==============================================
// FILE: DTOs/WebSocket/MessageReceivedData.cs
// Mô tả: Data khi nhận tin nhắn mới (gửi đến receiver)
// ==============================================
namespace ChatServer.DTOs.WebSocket
{
    public class MessageReceivedData
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public string SenderNickname { get; set; } = string.Empty;
        public int? ReceiverId { get; set; }
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

// Tác dụng: Server gửi lại client khi có tin nhắn mới.