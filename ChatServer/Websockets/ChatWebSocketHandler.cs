// ==============================================
// FILE: WebSockets/ChatWebSocketHandler.cs
// Mô tả: Xử lý WebSocket messages (core logic)
// ==============================================

// Tác dụng: Nhận message từ client, xử lý logic, gọi service, gửi response.

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ChatServer.Services;
using ChatServer.DTOs.WebSocket;
using Microsoft.EntityFrameworkCore;
using ChatServer.Data;

namespace ChatServer.WebSockets
{
    public class ChatWebSocketHandler
    {
        private readonly WebSocketManager _manager;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatWebSocketHandler> _logger;

        public ChatWebSocketHandler(
            WebSocketManager manager,
            IServiceProvider serviceProvider,
            ILogger<ChatWebSocketHandler> logger)
        {
            _manager = manager;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        // THÊM JSON OPTIONS Ở ĐẦU CLASS
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        // ===== XỬ LÝ CONNECTION =====
        public async Task HandleConnectionAsync(int userId, WebSocket socket)
        {
            // Thêm connection vào manager
            _manager.AddConnection(userId, socket);

            // Broadcast user online status đến tất cả users khác
            await BroadcastUserOnlineAsync(userId, true);

            try
            {
                // Loop vô hạn để nhận messages
                var buffer = new byte[1024 * 4];
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    // Parse message
                    var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessageAsync(userId, messageJson);
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning($"WebSocket exception for user {userId}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling WebSocket for user {userId}: {ex.Message}");
            }
            finally
            {
                // Cleanup khi disconnect
                await HandleDisconnectAsync(userId, socket);
            }
        }

        // ===== XỬ LÝ MESSAGE =====
        private async Task HandleMessageAsync(int senderId, string messageJson)
        {
            try
            {
                var wsMessage = JsonSerializer.Deserialize<WebSocketMessage>(messageJson, _jsonOptions);
                if (wsMessage == null || string.IsNullOrEmpty(wsMessage.Type))
                {
                    _logger.LogWarning($"Invalid message from user {senderId}");
                    return;
                }

                _logger.LogInformation($"Received message type '{wsMessage.Type}' from user {senderId}");

                switch (wsMessage.Type.ToLower())
                {
                    case "private_message":
                        await HandlePrivateMessageAsync(senderId, wsMessage.Data);
                        break;

                    case "group_message":
                        await HandleGroupMessageAsync(senderId, wsMessage.Data);
                        break;

                    case "typing":
                        await HandleTypingIndicatorAsync(senderId, wsMessage.Data);
                        break;

                    case "mark_read":
                        await HandleMarkReadAsync(senderId, wsMessage.Data);
                        break;

                    default:
                        _logger.LogWarning($"Unknown message type: {wsMessage.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling message: {ex.Message}");
            }
        }

        // ===== XỬ LÝ TIN NHẮN CÁ NHÂN =====
        private async Task HandlePrivateMessageAsync(int senderId, object? data)
        {
            using var scope = _serviceProvider.CreateScope();
            var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var messageData = JsonSerializer.Deserialize<PrivateMessageData>(
                    JsonSerializer.Serialize(data),
                    _jsonOptions
                );

                if (messageData == null)
                {
                    return;
                }

                // Lưu message vào database
                var result = await messageService.SendPrivateMessageAsync(
                    senderId,
                    messageData.ReceiverId,
                    messageData.Content
                );

                if (!result.Success || result.Data == null)
                {
                    return;
                }

                // Gửi message đến receiver (nếu online)
                var messageReceived = new WebSocketMessage
                {
                    Type = "private_message",
                    Data = new MessageReceivedData
                    {
                        MessageId = result.Data.Id,
                        SenderId = result.Data.SenderId,
                        SenderNickname = result.Data.SenderNickname,
                        ReceiverId = result.Data.ReceiverId,
                        Content = result.Data.Content,
                        Timestamp = result.Data.Timestamp
                    }
                };

                await _manager.SendToUserAsync(messageData.ReceiverId, messageReceived);

                // Echo message về sender (để confirm đã gửi)
                await _manager.SendToUserAsync(senderId, messageReceived);

                _logger.LogInformation($"Private message sent: {senderId} -> {messageData.ReceiverId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling private message: {ex.Message}");
            }
        }

        // ===== XỬ LÝ TIN NHẮN NHÓM =====
        private async Task HandleGroupMessageAsync(int senderId, object? data)
        {
            using var scope = _serviceProvider.CreateScope();
            var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var messageData = JsonSerializer.Deserialize<GroupMessageData>(
                    JsonSerializer.Serialize(data)
                );

                if (messageData == null)
                {
                    return;
                }

                // Lưu message vào database
                var result = await messageService.SendGroupMessageAsync(
                    senderId,
                    messageData.GroupId,
                    messageData.Content
                );

                if (!result.Success || result.Data == null)
                {
                    return;
                }

                // Lấy danh sách members trong nhóm
                var memberIds = await context.GroupMembers
                    .Where(gm => gm.GroupId == messageData.GroupId)
                    .Select(gm => gm.UserId)
                    .ToListAsync();

                // Gửi message đến tất cả members (kể cả sender)
                var messageReceived = new WebSocketMessage
                {
                    Type = "group_message",
                    Data = new MessageReceivedData
                    {
                        MessageId = result.Data.Id,
                        SenderId = result.Data.SenderId,
                        SenderNickname = result.Data.SenderNickname,
                        GroupId = result.Data.GroupId,
                        GroupName = result.Data.GroupName,
                        Content = result.Data.Content,
                        Timestamp = result.Data.Timestamp
                    }
                };

                await _manager.SendToUsersAsync(memberIds, messageReceived);

                _logger.LogInformation($"Group message sent: {senderId} -> Group {messageData.GroupId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling group message: {ex.Message}");
            }
        }

        // ===== XỬ LÝ TYPING INDICATOR =====
        private async Task HandleTypingIndicatorAsync(int senderId, object? data)
        {
            try
            {
                var typingData = JsonSerializer.Deserialize<TypingIndicatorData>(
                    JsonSerializer.Serialize(data)
                );

                if (typingData == null)
                {
                    return;
                }

                var typingMessage = new WebSocketMessage
                {
                    Type = "typing",
                    Data = new
                    {
                        UserId = senderId,
                        ReceiverId = typingData.ReceiverId,
                        GroupId = typingData.GroupId,
                        IsTyping = typingData.IsTyping
                    }
                };

                // Nếu typing cá nhân
                if (typingData.ReceiverId.HasValue)
                {
                    await _manager.SendToUserAsync(typingData.ReceiverId.Value, typingMessage);
                }
                // Nếu typing trong nhóm
                else if (typingData.GroupId.HasValue)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var memberIds = await context.GroupMembers
                        .Where(gm => gm.GroupId == typingData.GroupId.Value && gm.UserId != senderId)
                        .Select(gm => gm.UserId)
                        .ToListAsync();

                    await _manager.SendToUsersAsync(memberIds, typingMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling typing indicator: {ex.Message}");
            }
        }

        // ===== XỬ LÝ MARK READ =====
        private async Task HandleMarkReadAsync(int userId, object? data)
        {
            using var scope = _serviceProvider.CreateScope();
            var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

            try
            {
                var markReadData = JsonSerializer.Deserialize<Dictionary<string, int>>(
                    JsonSerializer.Serialize(data)
                );

                if (markReadData == null || !markReadData.ContainsKey("senderId"))
                {
                    return;
                }

                var senderId = markReadData["senderId"];
                await messageService.MarkMessagesAsReadAsync(userId, senderId);

                _logger.LogInformation($"Messages marked as read: User {userId} from {senderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling mark read: {ex.Message}");
            }
        }

        // ===== BROADCAST USER ONLINE STATUS =====
        private async Task BroadcastUserOnlineAsync(int userId, bool isOnline)
        {
            var statusMessage = new WebSocketMessage
            {
                Type = isOnline ? "user_online" : "user_offline",
                Data = new OnlineStatusData
                {
                    UserId = userId,
                    IsOnline = isOnline,
                    LastSeen = DateTime.Now
                }
            };

            await _manager.BroadcastToAllExceptAsync(userId, statusMessage);
        }

        // ===== XỬ LÝ DISCONNECT =====
        private async Task HandleDisconnectAsync(int userId, WebSocket socket)
        {
            // Update LastSeen trong database
            using var scope = _serviceProvider.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            await userService.UpdateLastSeenAsync(userId);

            // Remove connection
            _manager.RemoveConnection(userId);

            // Broadcast user offline status
            await BroadcastUserOnlineAsync(userId, false);

            // Close socket nếu chưa đóng
            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    CancellationToken.None
                );
            }

            _logger.LogInformation($"User {userId} disconnected");
        }
    }
}