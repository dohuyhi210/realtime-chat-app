// ==============================================
// FILE: DTOs/ChatHistoryResponse.cs
// Mô tả: DTO cho response lịch sử chat
// ==============================================
namespace ChatServer.DTOs.Message
{
    public class ChatHistoryResponse
    {
        public bool Success { get; set; } = true;
        public int Count { get; set; }
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
        public int? OtherUserId { get; set; }      // Nếu là chat cá nhân
        public string? OtherUserNickname { get; set; }
        public int? GroupId { get; set; }          // Nếu là chat nhóm
        public string? GroupName { get; set; }
    }
}