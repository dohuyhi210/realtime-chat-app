namespace ChatServer.DTOs.User
{
    public class OnlineStatusDto
    {
        public int UserId { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class OnlineStatusResponse
    {
        public bool Success { get; set; } = true;
        public List<OnlineStatusDto> Statuses { get; set; } = new List<OnlineStatusDto>();
    }
}