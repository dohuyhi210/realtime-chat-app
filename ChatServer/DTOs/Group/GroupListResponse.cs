// ==============================================
// FILE: DTOs/Group/GroupListResponse.cs
// Mô tả: DTO cho response danh sách nhóm
// ==============================================
namespace ChatServer.DTOs.Group
{
    public class GroupListResponse
    {
        public bool Success { get; set; } = true;
        public int Count { get; set; }
        public List<GroupDto> Groups { get; set; } = new List<GroupDto>();
    }
}