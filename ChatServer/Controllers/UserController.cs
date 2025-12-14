// ==============================================
// FILE: Controllers/UserController.cs
// Mô tả: API endpoints cho quản lý users
// ==============================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChatServer.Services;
using ChatServer.DTOs;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // Tất cả endpoints đều cần authentication
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: api/user
        // Tác dụng: Lấy danh sách tất cả users (trừ mình)
        // Header: Authorization: Bearer {token}
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var currentUserId = int.Parse(userIdClaim.Value);

                // Gọi service
                var result = await _userService.GetAllUsersAsync(currentUserId);

                _logger.LogInformation($"User {currentUserId} fetched user list");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllUsers: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/user/{id}
        // Tác dụng: Lấy thông tin chi tiết 1 user
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = $"User {id} not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUserById: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/user/me
        // Tác dụng: Lấy thông tin user hiện tại (từ token)
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var currentUserId = int.Parse(userIdClaim.Value);
                var user = await _userService.GetUserByIdAsync(currentUserId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCurrentUser: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/user/status
        // Tác dụng: Lấy trạng thái online của nhiều users
        // Body: { "userIds": [1, 2, 3] }
        [HttpPost("status")]
        public async Task<IActionResult> GetOnlineStatus([FromBody] List<int> userIds)
        {
            try
            {
                if (userIds == null || userIds.Count == 0)
                {
                    return BadRequest(new { message = "UserIds is required" });
                }

                var result = await _userService.GetOnlineStatusAsync(userIds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetOnlineStatus: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: api/user/lastseen
        // Tác dụng: Cập nhật LastSeen của user hiện tại (dùng khi user có activity)
        [HttpPut("lastseen")]
        public async Task<IActionResult> UpdateLastSeen()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var currentUserId = int.Parse(userIdClaim.Value);
                await _userService.UpdateLastSeenAsync(currentUserId);

                return Ok(new { message = "LastSeen updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateLastSeen: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/user/test
        // Tác dụng: Test endpoint (không cần authentication)
        [HttpGet("test")]
        [AllowAnonymous]  // Cho phép access không cần token
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "User API is working!",
                timestamp = DateTime.Now
            });
        }
    }
}