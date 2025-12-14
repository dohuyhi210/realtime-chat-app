// ==============================================
// FILE: Controllers/MessageController.cs
// Mô tả: API endpoints cho quản lý messages
// ==============================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChatServer.Services;
using ChatServer.DTOs.Message;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // Tất cả endpoints đều cần authentication
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessageController> _logger;

        public MessageController(IMessageService messageService, ILogger<MessageController> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        // POST: api/message/send
        // Tác dụng: Gửi tin nhắn (cá nhân hoặc nhóm)
        // Body: { "receiverId": 2, "content": "Hello" } hoặc { "groupId": 1, "content": "Hi team" }
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                // Validate: Phải có ReceiverId HOẶC GroupId
                if (!request.ReceiverId.HasValue && !request.GroupId.HasValue)
                {
                    return BadRequest(new { message = "Either receiverId or groupId is required" });
                }

                if (request.ReceiverId.HasValue && request.GroupId.HasValue)
                {
                    return BadRequest(new { message = "Cannot have both receiverId and groupId" });
                }

                // Validate input
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Lấy senderId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var senderId = int.Parse(userIdClaim.Value);

                // Gọi service
                SendMessageResponse result;

                if (request.ReceiverId.HasValue)
                {
                    // Tin nhắn cá nhân
                    result = await _messageService.SendPrivateMessageAsync(
                        senderId,
                        request.ReceiverId.Value,
                        request.Content
                    );
                }
                else
                {
                    // Tin nhắn nhóm
                    result = await _messageService.SendGroupMessageAsync(
                        senderId,
                        request.GroupId!.Value,
                        request.Content
                    );
                }

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendMessage: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/message/private/{userId}
        // Tác dụng: Lấy lịch sử chat với 1 user
        // Query params: ?skip=0&take=50
        [HttpGet("private/{userId}")]
        public async Task<IActionResult> GetPrivateChatHistory(
    int userId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50)
        {
            try
            {
                // Lấy currentUserId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var currentUserId = int.Parse(userIdClaim.Value);

                // Tính skip từ page
                int skip = (page - 1) * pageSize;

                // Gọi service để lấy tin nhắn
                var result = await _messageService.GetPrivateChatHistoryAsync(
                    currentUserId,
                    userId,
                    skip,
                    pageSize
                );

                // Gọi service để lấy tổng số tin nhắn
                var totalMessages = await _messageService.GetPrivateMessageCountAsync(currentUserId, userId);
                var totalPages = (int)Math.Ceiling(totalMessages / (double)pageSize);

                // SỬA RESPONSE STRUCTURE ĐỂ KHỚP VỚI FRONTEND
                return Ok(new
                {
                    success = result.Success,
                    count = result.Count,
                    messages = result.Messages,
                    otherUserId = result.OtherUserId,
                    otherUserNickname = result.OtherUserNickname,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalMessages = totalMessages,
                        totalPages = totalPages,
                        hasNextPage = page < totalPages,
                        hasPrevPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPrivateChatHistory: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/message/group/{groupId}
        // Tác dụng: Lấy lịch sử chat nhóm
        // Query params: ?skip=0&take=50
        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetGroupChatHistory(
            int groupId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50)
        {
            try
            {
                var result = await _messageService.GetGroupChatHistoryAsync(
                    groupId,
                    skip,
                    take
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetGroupChatHistory: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: api/message/mark-read
        // Tác dụng: Đánh dấu đã đọc tất cả tin nhắn từ 1 user
        // Body: { "senderId": 2 }
        [HttpPut("mark-read")]
        public async Task<IActionResult> MarkMessagesAsRead([FromBody] MarkReadRequest request)
        {
            try
            {
                // Lấy currentUserId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var currentUserId = int.Parse(userIdClaim.Value);

                // Gọi service
                var success = await _messageService.MarkMessagesAsReadAsync(
                    currentUserId,
                    request.SenderId
                );

                if (!success)
                {
                    return StatusCode(500, new { message = "Error marking messages as read" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Messages marked as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in MarkMessagesAsRead: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/message/unread-count
        // Tác dụng: Đếm số tin nhắn chưa đọc
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                // Lấy currentUserId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var currentUserId = int.Parse(userIdClaim.Value);

                // Gọi service
                var result = await _messageService.GetUnreadCountAsync(currentUserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUnreadCount: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/message/test
        // Tác dụng: Test endpoint
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Message API is working!",
                timestamp = DateTime.Now
            });
        }
    }
}