using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatServer.Models
{
    [Table("Groups")]
    public class Group
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string GroupName { get; set; } = string.Empty;

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan hệ: Người tạo nhóm
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        // Quan hệ 1-N: 1 Group có nhiều thành viên
        public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();

        // Quan hệ 1-N: 1 Group có nhiều tin nhắn
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}

// Ý nghĩa: Đại diện cho phòng chat nhóm, quản lý thành viên và tin nhắn trong nhóm.