// ==============================================
// FILE: Services/UserService.cs
// Mô tả: Service xử lý logic liên quan đến Users
// ==============================================
using Microsoft.EntityFrameworkCore;
using ChatServer.Data;
using ChatServer.Models;
using ChatServer.DTOs.User;

namespace ChatServer.Services
{
    public interface IUserService
    {
        Task<UserListResponse> GetAllUsersAsync(int currentUserId);
        Task<UserDetailDto?> GetUserByIdAsync(int userId);
        Task<OnlineStatusResponse> GetOnlineStatusAsync(List<int> userIds);
        Task UpdateLastSeenAsync(int userId);
        string CalculateOfflineTime(DateTime lastSeen);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== LẤY DANH SÁCH TẤT CẢ USERS (TRỪ MÌNH) =====
        public async Task<UserListResponse> GetAllUsersAsync(int currentUserId)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.Id != currentUserId)  // Loại trừ user hiện tại
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Nickname = u.Nickname,
                        LastSeen = u.LastSeen,
                        // Tính IsOnline: nếu LastSeen < 30 giây = online
                        IsOnline = (DateTime.Now - u.LastSeen).TotalSeconds < 30,
                        OfflineTimeText = "" // Sẽ tính sau
                    })
                    .ToListAsync();

                // Tính OfflineTimeText cho từng user
                foreach (var user in users)
                {
                    user.OfflineTimeText = user.IsOnline
                        ? "Online"
                        : CalculateOfflineTime(user.LastSeen);
                }

                // Sort: Online lên trên, Offline xuống dưới
                var sortedUsers = users
                    .OrderByDescending(u => u.IsOnline)
                    .ThenByDescending(u => u.LastSeen)
                    .ToList();

                return new UserListResponse
                {
                    Success = true,
                    Count = sortedUsers.Count,
                    Users = sortedUsers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting users: {ex.Message}");
                return new UserListResponse
                {
                    Success = false,
                    Count = 0,
                    Users = new List<UserDto>()
                };
            }
        }

        // ===== LẤY THÔNG TIN CHI TIẾT 1 USER =====
        public async Task<UserDetailDto?> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new UserDetailDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Nickname = u.Nickname,
                        LastSeen = u.LastSeen,
                        IsOnline = (DateTime.Now - u.LastSeen).TotalSeconds < 30,
                        CreatedAt = u.CreatedAt,
                        TotalMessagesSent = u.SentMessages.Count,
                        TotalMessagesReceived = u.ReceivedMessages.Count,
                        OfflineTimeText = ""
                    })
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    user.OfflineTimeText = user.IsOnline
                        ? "Online"
                        : CalculateOfflineTime(user.LastSeen);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user {userId}: {ex.Message}");
                return null;
            }
        }

        // ===== LẤY TRẠNG THÁI ONLINE CỦA NHIỀU USERS =====
        public async Task<OnlineStatusResponse> GetOnlineStatusAsync(List<int> userIds)
        {
            try
            {
                var statuses = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new OnlineStatusDto
                    {
                        UserId = u.Id,
                        IsOnline = (DateTime.Now - u.LastSeen).TotalSeconds < 30,
                        LastSeen = u.LastSeen
                    })
                    .ToListAsync();

                return new OnlineStatusResponse
                {
                    Success = true,
                    Statuses = statuses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting online status: {ex.Message}");
                return new OnlineStatusResponse
                {
                    Success = false,
                    Statuses = new List<OnlineStatusDto>()
                };
            }
        }

        // ===== CẬP NHẬT LASTSEEN KHI USER HOẠT ĐỘNG =====
        public async Task UpdateLastSeenAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.LastSeen = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating LastSeen for user {userId}: {ex.Message}");
            }
        }

        // ===== TÍNH THỜI GIAN OFFLINE =====
        public string CalculateOfflineTime(DateTime lastSeen)
        {
            var timeSpan = DateTime.Now - lastSeen;

            if (timeSpan.TotalMinutes < 1)
                return "Offline vừa xong";

            if (timeSpan.TotalMinutes < 60)
                return $"Offline {(int)timeSpan.TotalMinutes} phút trước";

            if (timeSpan.TotalHours < 24)
                return $"Offline {(int)timeSpan.TotalHours} giờ trước";

            if (timeSpan.TotalDays < 7)
                return $"Offline {(int)timeSpan.TotalDays} ngày trước";

            if (timeSpan.TotalDays < 30)
                return $"Offline {(int)(timeSpan.TotalDays / 7)} tuần trước";

            return $"Offline {(int)(timeSpan.TotalDays / 30)} tháng trước";
        }
    }
}