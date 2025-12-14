// ==============================================
// FILE: DTOs/WebSocket/WebSocketMessage.cs
// Mô tả: DTO chung cho tất cả WebSocket messages
// ==============================================
namespace ChatServer.DTOs.WebSocket
{
    public class WebSocketMessage
    {
        public string Type { get; set; } = string.Empty;  // "private_message", "group_message", "typing", "online", "offline"
        public object? Data { get; set; }
    }
}

// Tác dụng: Bao bọc mọi message, xác định loại và chứa data.