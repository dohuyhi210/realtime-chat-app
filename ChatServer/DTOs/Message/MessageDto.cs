// ==============================================
// FILE: DTOs/MessageDto.cs
// Mô tả: DTO cho tin nhắn (response)
// ==============================================
namespace ChatServer.DTOs.Message
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderNickname { get; set; } = string.Empty;
        public int? ReceiverId { get; set; }
        public string? ReceiverNickname { get; set; }
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
}