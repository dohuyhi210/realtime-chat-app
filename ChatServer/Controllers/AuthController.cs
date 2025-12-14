// ==============================================
// FILE: Controllers/AuthController.cs
// Mô tả: API endpoints cho đăng ký, đăng nhập
// ==============================================
using Microsoft.AspNetCore.Mvc;
using ChatServer.Services;
using ChatServer.DTOs.Auth;
using ChatServer.DTOs.Common;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // POST: api/auth/register
        // Tác dụng: Đăng ký tài khoản mới
        // Body: { "username": "alice", "password": "123456", "nickname": "Alice" }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors
                });
            }

            // Gọi service
            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"New user registered: {request.Username}");

            return Ok(result);
        }

        // POST: api/auth/login
        // Tác dụng: Đăng nhập
        // Body: { "username": "alice", "password": "123456" }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors
                });
            }

            // Gọi service
            var result = await _authService.LoginAsync(request);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            _logger.LogInformation($"User logged in: {request.Username}");

            return Ok(result);
        }

        // GET: api/auth/test
        // Tác dụng: Test endpoint (không cần authentication)
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Auth API is working!",
                timestamp = DateTime.Now
            });
        }
    }
}