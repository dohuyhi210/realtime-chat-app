// ==============================================
// FILE: Controllers/GroupController.cs
// Mô tả: API endpoints cho quản lý groups
// ==============================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChatServer.Services;
using ChatServer.DTOs.Group;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // Tất cả endpoints đều cần authentication
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly ILogger<GroupController> _logger;

        public GroupController(IGroupService groupService, ILogger<GroupController> logger)
        {
            _groupService = groupService;
            _logger = logger;
        }

        // POST: api/group
        // Tác dụng: Tạo nhóm mới
        // Body: { "groupName": "Team Project", "memberIds": [2, 3] }
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            try
            {
                // Validate
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Lấy creatorId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var creatorId = int.Parse(userIdClaim.Value);

                // Gọi service
                var result = await _groupService.CreateGroupAsync(
                    creatorId,
                    request.GroupName,
                    request.MemberIds
                );

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateGroup: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/group
        // Tác dụng: Lấy danh sách nhóm của user hiện tại
        [HttpGet]
        public async Task<IActionResult> GetUserGroups()
        {
            try
            {
                // Lấy userId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userId = int.Parse(userIdClaim.Value);

                // Gọi service
                var result = await _groupService.GetUserGroupsAsync(userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetUserGroups: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/group/{id}
        // Tác dụng: Lấy thông tin chi tiết nhóm
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGroupDetail(int id)
        {
            try
            {
                // Lấy userId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userId = int.Parse(userIdClaim.Value);

                // Gọi service
                var result = await _groupService.GetGroupDetailAsync(id, userId);

                if (result == null)
                {
                    return NotFound(new { message = "Group not found or you are not a member" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetGroupDetail: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/group/{id}/members
        // Tác dụng: Thêm thành viên vào nhóm
        // Body: { "userIds": [4, 5] }
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMembers(int id, [FromBody] AddMemberRequest request)
        {
            try
            {
                // Validate
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Lấy requesterId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var requesterId = int.Parse(userIdClaim.Value);

                // Gọi service
                var success = await _groupService.AddMembersAsync(id, request.UserIds, requesterId);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to add members. You might not be the group creator." });
                }

                return Ok(new
                {
                    success = true,
                    message = "Members added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddMembers: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/group/{id}/members/{userId}
        // Tác dụng: Xóa thành viên khỏi nhóm
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            try
            {
                // Lấy requesterId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var requesterId = int.Parse(userIdClaim.Value);

                // Gọi service
                var success = await _groupService.RemoveMemberAsync(id, userId, requesterId);

                if (!success)
                {
                    return BadRequest(new
                    {
                        message = "Failed to remove member. You might not be the group creator or trying to remove yourself."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Member removed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RemoveMember: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/group/{id}/leave
        // Tác dụng: Rời khỏi nhóm
        [HttpPost("{id}/leave")]
        public async Task<IActionResult> LeaveGroup(int id)
        {
            try
            {
                // Lấy userId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userId = int.Parse(userIdClaim.Value);

                // Gọi service
                var success = await _groupService.LeaveGroupAsync(id, userId);

                if (!success)
                {
                    return BadRequest(new
                    {
                        message = "Failed to leave group. You might be the group creator (delete the group instead)."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Left group successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in LeaveGroup: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/group/{id}
        // Tác dụng: Xóa nhóm (chỉ creator)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            try
            {
                // Lấy userId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userId = int.Parse(userIdClaim.Value);

                // Gọi service
                var success = await _groupService.DeleteGroupAsync(id, userId);

                if (!success)
                {
                    return BadRequest(new
                    {
                        message = "Failed to delete group. You might not be the group creator."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Group deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeleteGroup: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/group/test
        // Tác dụng: Test endpoint
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Group API is working!",
                timestamp = DateTime.Now
            });
        }
    }
}