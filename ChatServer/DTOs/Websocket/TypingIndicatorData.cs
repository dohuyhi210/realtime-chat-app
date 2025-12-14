// ==============================================
// FILE: DTOs/WebSocket/TypingIndicatorData.cs
// Mô tả: Data cho typing indicator
// ==============================================
namespace ChatServer.DTOs.WebSocket
{
    public class TypingIndicatorData
    {
        public int? ReceiverId { get; set; }  // Null nếu typing trong nhóm
        public int? GroupId { get; set; }     // Null nếu typing cá nhân
        public bool IsTyping { get; set; }
    }
}

// Tác dụng: Hiển thị "đang gõ..." cho người nhận.