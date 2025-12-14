// ==============================================
// FILE: DTOs/SendMessageRequest.cs
// Mô tả: DTO cho request gửi tin nhắn
// ==============================================
using System.ComponentModel.DataAnnotations;

namespace ChatServer.DTOs.Message
{
    public class SendMessageRequest
    {
        public int? ReceiverId { get; set; }  // Null nếu gửi vào nhóm

        public int? GroupId { get; set; }     // Null nếu gửi cho cá nhân

        [Required(ErrorMessage = "Content is required")]
        [MaxLength(5000, ErrorMessage = "Content cannot exceed 5000 characters")]
        public string Content { get; set; } = string.Empty;
    }
}