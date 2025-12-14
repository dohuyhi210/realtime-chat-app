// ==============================================
// FILE: DTOs/Group/RemoveMemberRequest.cs
// Mô tả: DTO cho request xóa thành viên
// ==============================================
using System.ComponentModel.DataAnnotations;

namespace ChatServer.DTOs.Group
{
    public class RemoveMemberRequest
    {
        [Required]
        public int UserId { get; set; }
    }
}