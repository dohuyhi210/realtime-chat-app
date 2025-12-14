using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatServer.Models
{
    [Table("GroupMembers")]
    public class GroupMember
    {
        [Required]
        public int GroupId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime JoinedAt { get; set; } = DateTime.Now;

        // Composite Primary Key: (GroupId + UserId)
        [ForeignKey("GroupId")]
        public virtual Group? Group { get; set; } // Nhóm mà user tham gia

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }  // User tham gia nhóm
    }
}

// Đặc điểm quan hệ nhiều-nhiều:
// Một User có thể tham gia nhiều Group
// Một Group có thể có nhiều User
// Bảng trung gian GroupMembers lưu thông tin bổ sung (JoinedAt)