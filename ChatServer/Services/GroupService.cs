// ==============================================
// FILE: Services/GroupService.cs
// Mô tả: Service xử lý logic liên quan đến Groups
// ==============================================
using Microsoft.EntityFrameworkCore;
using ChatServer.Data;
using ChatServer.DTOs.Group;
using ChatServer.Models;

namespace ChatServer.Services
{
    public interface IGroupService
    {
        Task<CreateGroupResponse> CreateGroupAsync(int creatorId, string groupName, List<int> memberIds);
        Task<GroupListResponse> GetUserGroupsAsync(int userId);
        Task<GroupDetailDto?> GetGroupDetailAsync(int groupId, int requesterId);
        Task<bool> AddMembersAsync(int groupId, List<int> userIds, int requesterId);
        Task<bool> RemoveMemberAsync(int groupId, int userId, int requesterId);
        Task<bool> DeleteGroupAsync(int groupId, int requesterId);
        Task<bool> LeaveGroupAsync(int groupId, int userId);
    }

    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GroupService> _logger;

        public GroupService(ApplicationDbContext context, ILogger<GroupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== TẠO NHÓM MỚI =====
        public async Task<CreateGroupResponse> CreateGroupAsync(int creatorId, string groupName, List<int> memberIds)
        {
            try
            {
                // Validate: Tất cả memberIds có tồn tại không
                var validUsers = await _context.Users
                    .Where(u => memberIds.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                if (validUsers.Count != memberIds.Count)
                {
                    var invalidIds = memberIds.Except(validUsers).ToList();
                    return new CreateGroupResponse
                    {
                        Success = false,
                        Message = $"Invalid user IDs: {string.Join(", ", invalidIds)}"
                    };
                }

                // Tạo group mới
                var group = new Group
                {
                    GroupName = groupName,
                    CreatedBy = creatorId,
                    CreatedAt = DateTime.Now
                };

                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

                // Thêm creator vào nhóm
                var creatorMember = new GroupMember
                {
                    GroupId = group.Id,
                    UserId = creatorId,
                    JoinedAt = DateTime.Now
                };
                _context.GroupMembers.Add(creatorMember);

                // Thêm các members khác (loại trừ creator nếu có trong list)
                var membersToAdd = memberIds.Where(id => id != creatorId).ToList();
                foreach (var memberId in membersToAdd)
                {
                    var member = new GroupMember
                    {
                        GroupId = group.Id,
                        UserId = memberId,
                        JoinedAt = DateTime.Now
                    };
                    _context.GroupMembers.Add(member);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Group created: {groupName} by user {creatorId}");

                // Load lại group detail
                var groupDetail = await GetGroupDetailAsync(group.Id, creatorId);

                return new CreateGroupResponse
                {
                    Success = true,
                    Message = "Group created successfully",
                    Group = groupDetail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating group: {ex.Message}");
                return new CreateGroupResponse
                {
                    Success = false,
                    Message = "Error creating group"
                };
            }
        }

        // ===== LẤY DANH SÁCH NHÓM CỦA USER =====
        public async Task<GroupListResponse> GetUserGroupsAsync(int userId)
        {
            try
            {
                var groups = await _context.GroupMembers
                    .Where(gm => gm.UserId == userId)
                    .Include(gm => gm.Group)
                        .ThenInclude(g => g.Creator)
                    .Include(gm => gm.Group)
                        .ThenInclude(g => g.Members)
                    .Include(gm => gm.Group)
                        .ThenInclude(g => g.Messages)
                    .Select(gm => new GroupDto
                    {
                        Id = gm.Group.Id,
                        GroupName = gm.Group.GroupName,
                        CreatedBy = gm.Group.CreatedBy,
                        CreatorNickname = gm.Group.Creator!.Nickname,
                        CreatedAt = gm.Group.CreatedAt,
                        MemberCount = gm.Group.Members.Count,
                        LastMessage = gm.Group.Messages
                            .OrderByDescending(m => m.Timestamp)
                            .Select(m => m.Content)
                            .FirstOrDefault(),
                        LastMessageTime = gm.Group.Messages
                            .OrderByDescending(m => m.Timestamp)
                            .Select(m => (DateTime?)m.Timestamp)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(g => g.LastMessageTime ?? g.CreatedAt)
                    .ToListAsync();

                return new GroupListResponse
                {
                    Success = true,
                    Count = groups.Count,
                    Groups = groups
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user groups: {ex.Message}");
                return new GroupListResponse
                {
                    Success = false,
                    Count = 0,
                    Groups = new List<GroupDto>()
                };
            }
        }

        // ===== LẤY CHI TIẾT NHÓM =====
        public async Task<GroupDetailDto?> GetGroupDetailAsync(int groupId, int requesterId)
        {
            try
            {
                // Check user có trong nhóm không
                var isMember = await _context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == requesterId);

                if (!isMember)
                {
                    _logger.LogWarning($"User {requesterId} is not a member of group {groupId}");
                    return null;
                }

                var group = await _context.Groups
                    .Include(g => g.Creator)
                    .Include(g => g.Members)
                        .ThenInclude(gm => gm.User)
                    .Include(g => g.Messages)
                    .FirstOrDefaultAsync(g => g.Id == groupId);

                if (group == null)
                {
                    return null;
                }

                var members = group.Members.Select(gm => new GroupMemberDto
                {
                    UserId = gm.UserId,
                    Username = gm.User!.Username,
                    Nickname = gm.User.Nickname,
                    JoinedAt = gm.JoinedAt,
                    IsOnline = (DateTime.Now - gm.User.LastSeen).TotalSeconds < 30,
                    IsCreator = gm.UserId == group.CreatedBy
                }).ToList();

                return new GroupDetailDto
                {
                    Id = group.Id,
                    GroupName = group.GroupName,
                    CreatedBy = group.CreatedBy,
                    CreatorNickname = group.Creator!.Nickname,
                    CreatedAt = group.CreatedAt,
                    MemberCount = group.Members.Count,
                    TotalMessages = group.Messages.Count,
                    Members = members
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting group detail: {ex.Message}");
                return null;
            }
        }

        // ===== THÊM THÀNH VIÊN =====
        public async Task<bool> AddMembersAsync(int groupId, List<int> userIds, int requesterId)
        {
            try
            {
                // Check requester có phải creator không
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    return false;
                }

                if (group.CreatedBy != requesterId)
                {
                    _logger.LogWarning($"User {requesterId} is not authorized to add members to group {groupId}");
                    return false;
                }

                // Validate users tồn tại
                var validUsers = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                if (validUsers.Count == 0)
                {
                    return false;
                }

                // Lấy danh sách members hiện tại
                var existingMembers = await _context.GroupMembers
                    .Where(gm => gm.GroupId == groupId)
                    .Select(gm => gm.UserId)
                    .ToListAsync();

                // Chỉ thêm users chưa có trong nhóm
                var newMemberIds = validUsers.Except(existingMembers).ToList();

                foreach (var userId in newMemberIds)
                {
                    var member = new GroupMember
                    {
                        GroupId = groupId,
                        UserId = userId,
                        JoinedAt = DateTime.Now
                    };
                    _context.GroupMembers.Add(member);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Added {newMemberIds.Count} members to group {groupId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding members: {ex.Message}");
                return false;
            }
        }

        // ===== XÓA THÀNH VIÊN =====
        public async Task<bool> RemoveMemberAsync(int groupId, int userId, int requesterId)
        {
            try
            {
                // Check requester có phải creator không
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    return false;
                }

                if (group.CreatedBy != requesterId)
                {
                    _logger.LogWarning($"User {requesterId} is not authorized to remove members from group {groupId}");
                    return false;
                }

                // Không thể xóa creator
                if (userId == group.CreatedBy)
                {
                    _logger.LogWarning($"Cannot remove creator from group {groupId}");
                    return false;
                }

                // Xóa member
                var member = await _context.GroupMembers
                    .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

                if (member == null)
                {
                    return false;
                }

                _context.GroupMembers.Remove(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Removed user {userId} from group {groupId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing member: {ex.Message}");
                return false;
            }
        }

        // ===== RỜI NHÓM =====
        public async Task<bool> LeaveGroupAsync(int groupId, int userId)
        {
            try
            {
                // Check user có phải creator không
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    return false;
                }

                if (group.CreatedBy == userId)
                {
                    _logger.LogWarning($"Creator cannot leave group {groupId}. Delete group instead.");
                    return false;
                }

                // Xóa member
                var member = await _context.GroupMembers
                    .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

                if (member == null)
                {
                    return false;
                }

                _context.GroupMembers.Remove(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} left group {groupId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error leaving group: {ex.Message}");
                return false;
            }
        }

        // ===== XÓA NHÓM =====
        public async Task<bool> DeleteGroupAsync(int groupId, int requesterId)
        {
            try
            {
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    return false;
                }

                // Chỉ creator mới xóa được nhóm
                if (group.CreatedBy != requesterId)
                {
                    _logger.LogWarning($"User {requesterId} is not authorized to delete group {groupId}");
                    return false;
                }

                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Group {groupId} deleted by user {requesterId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting group: {ex.Message}");
                return false;
            }
        }
    }
}