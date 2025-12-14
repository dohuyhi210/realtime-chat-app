// ==============================================
// FILE: DTOs/Group/CreateGroupRequest.cs
// Mô tả: DTO cho request tạo nhóm mới
// ==============================================
using System.ComponentModel.DataAnnotations;

namespace ChatServer.DTOs.Group
{
    public class CreateGroupRequest
    {
        [Required(ErrorMessage = "Group name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Group name must be between 2 and 100 characters")]
        public string GroupName { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one member is required")]
        [MinLength(1, ErrorMessage = "Group must have at least one member")]
        public List<int> MemberIds { get; set; } = new List<int>();
    }
}
