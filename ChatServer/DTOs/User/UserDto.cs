// ==============================================
// FILE: DTOs/UserDto.cs
// Mô tả: DTO cho danh sách users (compact)
// ==============================================
namespace ChatServer.DTOs.User
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; }
        public bool IsOnline { get; set; }
        public string OfflineTimeText { get; set; } = string.Empty;
    }
}