// ==============================================
// FILE: DTOs/Group/GroupDetailDto.cs
// Mô tả: DTO cho thông tin chi tiết group
// ==============================================
namespace ChatServer.DTOs.Group
{
    public class GroupDetailDto
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public string CreatorNickname { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
        public int TotalMessages { get; set; }
        public List<GroupMemberDto> Members { get; set; } = new List<GroupMemberDto>();
    }
}