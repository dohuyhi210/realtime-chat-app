// ==============================================
// FILE: DTOs/WebSocket/PrivateMessageData.cs
// Mô tả: Data cho tin nhắn cá nhân
// ==============================================
namespace ChatServer.DTOs.WebSocket
{
    public class PrivateMessageData
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}

// Tác dụng: Client gửi tin nhắn cho người khác.