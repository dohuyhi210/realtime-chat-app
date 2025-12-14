// ==============================================
// FILE: DTOs/UserListResponse.cs
// Mô tả: DTO cho response danh sách users
// ==============================================
namespace ChatServer.DTOs.User
{
    public class UserListResponse
    {
        public bool Success { get; set; } = true;
        public int Count { get; set; }
        public List<UserDto> Users { get; set; } = new List<UserDto>();
    }
}