// ==============================================
// FILE: Services/MessageService.cs
// Mô tả: Service xử lý logic liên quan đến Messages
// ==============================================
using Microsoft.EntityFrameworkCore;
using ChatServer.Data;
using ChatServer.Models;
using ChatServer.DTOs.Message;

namespace ChatServer.Services
{
    public interface IMessageService
    {
        Task<SendMessageResponse> SendPrivateMessageAsync(int senderId, int receiverId, string content);
        Task<SendMessageResponse> SendGroupMessageAsync(int senderId, int groupId, string content);
        Task<ChatHistoryResponse> GetPrivateChatHistoryAsync(int userId1, int userId2, int skip = 0, int take = 50);
        Task<ChatHistoryResponse> GetGroupChatHistoryAsync(int groupId, int skip = 0, int take = 50);
        Task<bool> MarkMessagesAsReadAsync(int receiverId, int senderId);
        Task<UnreadCountResponse> GetUnreadCountAsync(int userId);

        Task<int> GetPrivateMessageCountAsync(int currentUserId, int otherUserId);
    }

    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MessageService> _logger;

        public MessageService(ApplicationDbContext context, ILogger<MessageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== GỬI TIN NHẮN CÁ NHÂN =====
        public async Task<SendMessageResponse> SendPrivateMessageAsync(int senderId, int receiverId, string content)
        {
            try
            {
                // Validate: Receiver có tồn tại không
                var receiver = await _context.Users.FindAsync(receiverId);
                if (receiver == null)
                {
                    return new SendMessageResponse
                    {
                        Success = false,
                        Message = $"User {receiverId} not found"
                    };
                }

                // Tạo message mới
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    GroupId = null,
                    Content = content,
                    Timestamp = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Load lại với navigation properties
                await _context.Entry(message)
                    .Reference(m => m.Sender)
                    .LoadAsync();
                await _context.Entry(message)
                    .Reference(m => m.Receiver)
                    .LoadAsync();

                _logger.LogInformation($"Private message sent: {senderId} -> {receiverId}");

                // Convert sang DTO
                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    SenderNickname = message.Sender!.Nickname,
                    ReceiverId = message.ReceiverId,
                    ReceiverNickname = message.Receiver!.Nickname,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    IsRead = message.IsRead
                };

                return new SendMessageResponse
                {
                    Success = true,
                    Message = "Message sent successfully",
                    Data = messageDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending private message: {ex.Message}");
                return new SendMessageResponse
                {
                    Success = false,
                    Message = "Error sending message"
                };
            }
        }

        // ===== GỬI TIN NHẮN NHÓM =====
        public async Task<SendMessageResponse> SendGroupMessageAsync(int senderId, int groupId, string content)
        {
            try
            {
                // Validate: Group có tồn tại không
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    return new SendMessageResponse
                    {
                        Success = false,
                        Message = $"Group {groupId} not found"
                    };
                }

                // Validate: User có trong nhóm không
                var isMember = await _context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == senderId);

                if (!isMember)
                {
                    return new SendMessageResponse
                    {
                        Success = false,
                        Message = "You are not a member of this group"
                    };
                }

                // Tạo message mới
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = null,
                    GroupId = groupId,
                    Content = content,
                    Timestamp = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Load lại với navigation properties
                await _context.Entry(message)
                    .Reference(m => m.Sender)
                    .LoadAsync();
                await _context.Entry(message)
                    .Reference(m => m.Group)
                    .LoadAsync();

                _logger.LogInformation($"Group message sent: {senderId} -> Group {groupId}");

                // Convert sang DTO
                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    SenderNickname = message.Sender!.Nickname,
                    GroupId = message.GroupId,
                    GroupName = message.Group!.GroupName,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    IsRead = message.IsRead
                };

                return new SendMessageResponse
                {
                    Success = true,
                    Message = "Message sent successfully",
                    Data = messageDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending group message: {ex.Message}");
                return new SendMessageResponse
                {
                    Success = false,
                    Message = "Error sending message"
                };
            }
        }

        // ===== LẤY LỊCH SỬ CHAT CÁ NHÂN =====
        public async Task<ChatHistoryResponse> GetPrivateChatHistoryAsync(int userId1, int userId2, int skip = 0, int take = 50)
        {
            try
            {
                // Lấy messages giữa 2 users
                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Where(m =>
                        (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                        (m.SenderId == userId2 && m.ReceiverId == userId1)
                    )
                    .OrderByDescending(m => m.Timestamp)
                    .Skip(skip)
                    .Take(take)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        SenderNickname = m.Sender!.Nickname,
                        ReceiverId = m.ReceiverId,
                        ReceiverNickname = m.Receiver!.Nickname,
                        Content = m.Content,
                        Timestamp = m.Timestamp,
                        IsRead = m.IsRead
                    })
                    .ToListAsync();

                // Reverse để tin nhắn cũ lên trên, mới xuống dưới
                messages.Reverse();

                // Lấy thông tin user kia
                var otherUser = await _context.Users.FindAsync(userId2);

                return new ChatHistoryResponse
                {
                    Success = true,
                    Count = messages.Count,
                    Messages = messages,
                    OtherUserId = userId2,
                    OtherUserNickname = otherUser?.Nickname
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting private chat history: {ex.Message}");
                return new ChatHistoryResponse
                {
                    Success = false,
                    Count = 0,
                    Messages = new List<MessageDto>()
                };
            }
        }

        // ===== LẤY LỊCH SỬ CHAT NHÓM =====
        public async Task<ChatHistoryResponse> GetGroupChatHistoryAsync(int groupId, int skip = 0, int take = 50)
        {
            try
            {
                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Group)
                    .Where(m => m.GroupId == groupId)
                    .OrderByDescending(m => m.Timestamp)
                    .Skip(skip)
                    .Take(take)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        SenderNickname = m.Sender!.Nickname,
                        GroupId = m.GroupId,
                        GroupName = m.Group!.GroupName,
                        Content = m.Content,
                        Timestamp = m.Timestamp,
                        IsRead = false  // Group messages không có IsRead
                    })
                    .ToListAsync();

                // Reverse để tin nhắn cũ lên trên
                messages.Reverse();

                // Lấy thông tin group
                var group = await _context.Groups.FindAsync(groupId);

                return new ChatHistoryResponse
                {
                    Success = true,
                    Count = messages.Count,
                    Messages = messages,
                    GroupId = groupId,
                    GroupName = group?.GroupName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting group chat history: {ex.Message}");
                return new ChatHistoryResponse
                {
                    Success = false,
                    Count = 0,
                    Messages = new List<MessageDto>()
                };
            }
        }

        // ===== ĐÁNH DẤU ĐÃ ĐỌC =====
        public async Task<bool> MarkMessagesAsReadAsync(int receiverId, int senderId)
        {
            try
            {
                // Update tất cả tin nhắn chưa đọc từ senderId gửi cho receiverId
                var unreadMessages = await _context.Messages
                    .Where(m =>
                        m.ReceiverId == receiverId &&
                        m.SenderId == senderId &&
                        m.IsRead == false
                    )
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Marked {unreadMessages.Count} messages as read for user {receiverId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking messages as read: {ex.Message}");
                return false;
            }
        }

        // ===== ĐẾM TIN NHẮN CHƯA ĐỌC =====
        public async Task<UnreadCountResponse> GetUnreadCountAsync(int userId)
        {
            try
            {
                // Đếm tổng số tin nhắn chưa đọc
                var totalUnread = await _context.Messages
                    .Where(m => m.ReceiverId == userId && m.IsRead == false)
                    .CountAsync();

                // Đếm theo từng sender
                var unreadByUser = await _context.Messages
                    .Where(m => m.ReceiverId == userId && m.IsRead == false)
                    .GroupBy(m => m.SenderId)
                    .Select(g => new
                    {
                        SenderId = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.SenderId, x => x.Count);

                return new UnreadCountResponse
                {
                    Success = true,
                    TotalUnread = totalUnread,
                    UnreadByUser = unreadByUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting unread count: {ex.Message}");
                return new UnreadCountResponse
                {
                    Success = false,
                    TotalUnread = 0,
                    UnreadByUser = new Dictionary<int, int>()
                };
            }
        }
        public async Task<int> GetPrivateMessageCountAsync(int currentUserId, int otherUserId)
        {
            return await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                           (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .CountAsync();
        }
    }
}