// ==============================================
// FILE: DTOs/Group/GroupMemberDto.cs
// Mô tả: DTO cho thành viên trong nhóm
// ==============================================
namespace ChatServer.DTOs.Group
{
    public class GroupMemberDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public bool IsOnline { get; set; }
        public bool IsCreator { get; set; }
    }
}