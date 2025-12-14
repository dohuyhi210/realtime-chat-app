// ==============================================
// FILE: DTOs/Group/AddMemberRequest.cs
// Mô tả: DTO cho request thêm thành viên
// ==============================================
using System.ComponentModel.DataAnnotations;

namespace ChatServer.DTOs.Group
{
    public class AddMemberRequest
    {
        [Required]
        public List<int> UserIds { get; set; } = new List<int>();
    }
}