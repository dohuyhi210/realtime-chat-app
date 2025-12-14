// ==============================================
// FILE: Services/AuthService.cs
// Mô tả: Service xử lý logic đăng ký, đăng nhập, JWT
// ==============================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ChatServer.Data;
using ChatServer.Models;
using ChatServer.DTOs.Auth;

namespace ChatServer.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        string GenerateJwtToken(User user);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // ===== ĐĂNG KÝ =====
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // 1. Kiểm tra username đã tồn tại chưa
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

                if (existingUser != null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // 2. Hash password bằng BCrypt
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // 3. Tạo user mới
                var newUser = new User
                {
                    Username = request.Username.ToLower(),
                    PasswordHash = passwordHash,
                    Nickname = request.Nickname,
                    LastSeen = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                // 4. Lưu vào database
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // 5. Generate JWT token
                var token = GenerateJwtToken(newUser);

                _logger.LogInformation($"User registered successfully: {newUser.Username}");

                // 6. Trả về response
                return new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = newUser.Id,
                        Username = newUser.Username,
                        Nickname = newUser.Nickname
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Registration error: {ex.Message}");
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during registration"
                };
            }
        }

        // ===== ĐĂNG NHẬP =====
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // 1. Tìm user theo username
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                // 2. Verify password bằng BCrypt
                var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

                if (!isPasswordValid)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                // 3. Cập nhật LastSeen
                user.LastSeen = DateTime.Now;
                await _context.SaveChangesAsync();

                // 4. Generate JWT token
                var token = GenerateJwtToken(user);

                _logger.LogInformation($"User logged in successfully: {user.Username}");

                // 5. Trả về response
                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Nickname = user.Nickname
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        // ===== GENERATE JWT TOKEN =====
        public string GenerateJwtToken(User user)
        {
            // 1. Lấy config từ appsettings.json
            var secretKey = _configuration["JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = _configuration["JwtSettings:Issuer"] ?? "ChatApp";
            var audience = _configuration["JwtSettings:Audience"] ?? "ChatAppUsers";
            var expirationHours = int.Parse(_configuration["JwtSettings:ExpirationHours"] ?? "24");

            // 2. Tạo claims (thông tin trong token)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("Nickname", user.Nickname),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique ID cho token
            };

            // 3. Tạo key từ secret
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 4. Tạo token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(expirationHours),
                signingCredentials: credentials
            );

            // 5. Encode token thành string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}