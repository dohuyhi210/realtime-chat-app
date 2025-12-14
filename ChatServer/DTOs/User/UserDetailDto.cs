// ==============================================
// FILE: DTOs/UserDetailDto.cs
// Mô tả: DTO cho thông tin chi tiết user
// ==============================================
namespace ChatServer.DTOs.User
{
    public class UserDetailDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; }
        public bool IsOnline { get; set; }
        public string OfflineTimeText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TotalMessagesSent { get; set; }
        public int TotalMessagesReceived { get; set; }
    }
}