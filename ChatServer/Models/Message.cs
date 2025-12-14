using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace ChatServer.Models
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        public int? ReceiverId { get; set; }  // Nullable - null nếu là tin nhắn nhóm

        public int? GroupId { get; set; }      // Nullable - null nếu là tin nhắn cá nhân

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required]
        public bool IsRead { get; set; } = false;

        // Navigation properties - cho phép truy xuất trực tiếp đối tượng liên quan
        [ForeignKey("SenderId")]
        public virtual User? Sender { get; set; } // Người gửi tin nhắn

        [ForeignKey("ReceiverId")]
        public virtual User? Receiver { get; set; } // Người nhận tin nhắn

        [ForeignKey("GroupId")]
        public virtual Group? Group { get; set; } // Nhóm nhận tin nhắn
    }
}


// Logic xác định loại tin nhắn:
// Tin nhắn cá nhân: ReceiverId có giá trị, GroupId = null
// Tin nhắn nhóm: GroupId có giá trị, ReceiverId = null
