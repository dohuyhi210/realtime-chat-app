// ==============================================
// FILE: DTOs/WebSocket/GroupMessageData.cs
// Mô tả: Data cho tin nhắn nhóm
// ==============================================
namespace ChatServer.DTOs.WebSocket
{
    public class GroupMessageData
    {
        public int GroupId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}

// Tác dụng: Client gửi tin nhắn vào nhóm.