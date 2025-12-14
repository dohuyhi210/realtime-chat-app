// ==============================================
// FILE: DTOs/Group/CreateGroupResponse.cs
// Mô tả: DTO cho response sau khi tạo nhóm
// ==============================================
namespace ChatServer.DTOs.Group
{
    public class CreateGroupResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public GroupDetailDto? Group { get; set; }
    }
}