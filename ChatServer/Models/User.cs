using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace ChatServer.Models
{
    [Table("Users")] // Table name in the database
    public class User
    {
        [Key]  // Primary Key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-increment
        public int Id { get; set; }

        [Required] // Not null
        [MaxLength(50)] // Max length constraint
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Nickname { get; set; } = string.Empty;

        [Required]
        public DateTime LastSeen { get; set; } = DateTime.Now;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan hệ 1-N: 1 User có thể gửi nhiều Message
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();

        // Quan hệ 1-N: 1 User có thể nhận nhiều Message 
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();

        // Quan hệ 1-N: 1 User có thể tạo nhiều Group
        public virtual ICollection<Group> CreatedGroups { get; set; } = new List<Group>();

        // Quan hệ 1-N: 1 User có thể tham gia nhiều Group
        public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    }
}
