// ==============================================
// FILE: Program.cs
// Mô tả: Entry point và configuration của ChatServer
// ==============================================
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ChatServer.Data;



var builder = WebApplication.CreateBuilder(args);

// ===== 1. CẤU HÌNH SERVICES =====

// Database Context với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Đăng ký Services
builder.Services.AddScoped<ChatServer.Services.IAuthService, ChatServer.Services.AuthService>();
builder.Services.AddScoped<ChatServer.Services.IUserService, ChatServer.Services.UserService>();
builder.Services.AddScoped<ChatServer.Services.IMessageService, ChatServer.Services.MessageService>();
builder.Services.AddScoped<ChatServer.Services.IGroupService, ChatServer.Services.GroupService>();

// WebSocket Services (Singleton để chia sẻ giữa các requests)
builder.Services.AddSingleton<ChatServer.WebSockets.WebSocketManager>();
builder.Services.AddSingleton<ChatServer.WebSockets.ChatWebSocketHandler>();

// Controllers
builder.Services.AddControllers();

// Swagger cho Development
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS - Cho phép client kết nối
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowChatClient", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)  // Cho phép mọi origin
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "ChatApp";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "ChatAppUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// ===== 2. BUILD APPLICATION =====
builder.WebHost.UseUrls("http://0.0.0.0:5000", "https://0.0.0.0:5001");
var app = builder.Build();

// ===== 3. CẤU HÌNH MIDDLEWARE =====

// Swagger chỉ cho Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS phải đứng đầu
app.UseCors("AllowChatClient");

// HTTPS và WebSocket
//app.UseHttpsRedirection();
app.UseWebSockets();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// WebSocket Endpoint - Xử lý kết nối realtime
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var token = context.Request.Query["token"].ToString();

            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing token");
                return;
            }

            try
            {
                // Validate JWT token
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(jwtSecretKey);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                }, out _);

                // Lấy UserId từ claims
                var userIdClaim = principal.FindFirst("UserId")?.Value
                    ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid token");
                    return;
                }

                // Chấp nhận kết nối WebSocket
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var handler = context.RequestServices.GetRequiredService<ChatServer.WebSockets.ChatWebSocketHandler>();

                Console.WriteLine($"✅ WebSocket connected: User {userId}");
                await handler.HandleConnectionAsync(userId, webSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WebSocket auth failed: {ex.Message}");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid token");
            }
        }
        else
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket connection required");
        }
    }
    else
    {
        await next();
    }
});

// Map API Controllers
app.MapControllers();

// ===== 4. KIỂM TRA KẾT NỐI DATABASE =====
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        if (dbContext.Database.CanConnect())
        {
            Console.WriteLine("✅ Kết nối database thành công!");
        }
        else
        {
            Console.WriteLine("❌ Không thể kết nối database!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Lỗi kết nối database: {ex.Message}");
    }
}

// ===== 5. CHẠY ỨNG DỤNG =====
Console.WriteLine("🚀 ChatServer đang khởi động...");
Console.WriteLine($"🌐 URL: https://localhost:5001");
Console.WriteLine($"📖 Swagger: https://localhost:5001/swagger");

app.Run();