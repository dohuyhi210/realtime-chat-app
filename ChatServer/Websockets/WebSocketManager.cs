// ==============================================
// FILE: WebSockets/WebSocketManager.cs
// Mô tả: Quản lý tất cả WebSocket connections
// ==============================================

// Tác dụng: Theo dõi ai đang online và gửi message đến đúng người.

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ChatServer.WebSockets
{
    public class WebSocketManager
    {
        // Thread-safe dictionary để lưu connections

        // Dictionary để lưu trữ: UserId → WebSocket connection
        private readonly ConcurrentDictionary<int, WebSocket> _connections = new();
        private readonly ILogger<WebSocketManager> _logger;

        public WebSocketManager(ILogger<WebSocketManager> logger)
        {
            _logger = logger;
        }

        // ===== THÊM CONNECTION =====
        // Khi user kết nối
        public void AddConnection(int userId, WebSocket socket)
        {
            _connections[userId] = socket; // Lưu: User  → WebSocket A
            _logger.LogInformation($"WebSocket connected: UserId {userId}. Total connections: {_connections.Count}");
        }

        // ===== XÓA CONNECTION =====
        public void RemoveConnection(int userId)
        {
            _connections.TryRemove(userId, out _); // Xóa connection của user
            _logger.LogInformation($"WebSocket disconnected: UserId {userId}. Total connections: {_connections.Count}");
        }

        // ===== LẤY CONNECTION CỦA 1 USER =====
        public WebSocket? GetConnection(int userId)
        {
            _connections.TryGetValue(userId, out var socket);
            return socket;
        }

        // ===== CHECK USER CÓ ONLINE KHÔNG =====
        public bool IsUserOnline(int userId)
        {
            return _connections.ContainsKey(userId);
        }

        // ===== LẤY DANH SÁCH USER IDS ĐANG ONLINE =====
        public List<int> GetOnlineUserIds()
        {
            return _connections.Keys.ToList();
        }

        // ===== GỬI MESSAGE ĐẾN 1 USER =====
        public async Task<bool> SendToUserAsync(int userId, object message)
        {
            var socket = GetConnection(userId);
            if (socket == null || socket.State != WebSocketState.Open)
            {
                return false;
            }

            try
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message to user {userId}: {ex.Message}");
                return false;
            }
        }

        // ===== GỬI MESSAGE ĐẾN NHIỀU USERS =====
        public async Task SendToUsersAsync(List<int> userIds, object message)
        {
            var tasks = userIds.Select(userId => SendToUserAsync(userId, message));
            await Task.WhenAll(tasks);
        }

        // ===== BROADCAST ĐẾN TẤT CẢ USERS ĐANG ONLINE =====
        public async Task BroadcastToAllAsync(object message)
        {
            var userIds = GetOnlineUserIds();
            await SendToUsersAsync(userIds, message);
        }

        // ===== BROADCAST ĐẾN TẤT CẢ TRỪ 1 USER =====
        public async Task BroadcastToAllExceptAsync(int excludeUserId, object message)
        {
            var userIds = GetOnlineUserIds().Where(id => id != excludeUserId).ToList();
            await SendToUsersAsync(userIds, message);
        }

        // ===== ĐÓNG TẤT CẢ CONNECTIONS (KHI SHUTDOWN SERVER) =====
        public async Task CloseAllConnectionsAsync()
        {
            _logger.LogInformation("Closing all WebSocket connections...");

            foreach (var kvp in _connections)
            {
                var socket = kvp.Value;
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Server shutting down",
                            CancellationToken.None
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error closing socket for user {kvp.Key}: {ex.Message}");
                    }
                }
            }

            _connections.Clear();
            _logger.LogInformation("All WebSocket connections closed.");
        }
    }
}