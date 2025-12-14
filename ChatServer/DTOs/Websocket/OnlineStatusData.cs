// ==============================================
// FILE: DTOs/WebSocket/OnlineStatusData.cs
// Mô tả: Data cho online/offline status
// ==============================================
namespace ChatServer.DTOs.WebSocket
{
    public class OnlineStatusData
    {
        public int UserId { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
    }
}

// Tác dụng: Thông báo user online/offline cho mọi người.