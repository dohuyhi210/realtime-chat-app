// ==============================================
// FILE: DTOs/Group/GroupDto.cs
// Mô tả: DTO cho group (compact)
// ==============================================
namespace ChatServer.DTOs.Group
{
    public class GroupDto
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public string CreatorNickname { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
    }
}
