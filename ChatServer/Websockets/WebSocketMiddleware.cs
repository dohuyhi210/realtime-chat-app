// ==============================================
// FILE: WebSockets/WebSocketMiddleware.cs
// Mô tả: Middleware để intercept WebSocket requests
// ==============================================

// Tác dụng: Authentication, chỉ cho phép user đã login kết nối WebSocket.

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ChatServer.WebSockets
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebSocketMiddleware> _logger;

        public WebSocketMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<WebSocketMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ChatWebSocketHandler handler)
        {
            // Check nếu là WebSocket request
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next(context);
                return;
            }

            // Check path phải là /ws
            if (context.Request.Path != "/ws")
            {
                context.Response.StatusCode = 404;
                return;
            }

            _logger.LogInformation("WebSocket request received");

            // Lấy token từ query string
            var token = context.Request.Query["token"].ToString();

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("WebSocket request without token");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing authentication token");
                return;
            }

            // Verify JWT token
            var userId = ValidateToken(token);
            if (userId == null)
            {
                _logger.LogWarning("WebSocket request with invalid token");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid authentication token");
                return;
            }

            _logger.LogInformation($"WebSocket authenticated: UserId {userId}");

            // Accept WebSocket connection
            var socket = await context.WebSockets.AcceptWebSocketAsync();

            // Xử lý connection
            await handler.HandleConnectionAsync(userId.Value, socket);
        }

        // ===== VALIDATE JWT TOKEN =====
        private int? ValidateToken(string token)
        {
            try
            {
                var secretKey = _configuration["JwtSettings:SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    return null;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating token: {ex.Message}");
                return null;
            }
        }
    }
}