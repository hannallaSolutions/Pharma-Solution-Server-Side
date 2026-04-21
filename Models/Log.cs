using ServerSide.Model;

namespace SearchTool_ServerSide.Models
{
    public class Log : IEntity
    {
        public int Id { get; set; }

        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }

        public User? User { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}