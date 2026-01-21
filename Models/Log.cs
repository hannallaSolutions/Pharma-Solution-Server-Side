using ServerSide.Model;

namespace SearchTool_ServerSide.Models
{
    public class Log : IEntity
    {
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public string Action { get; set; }
        public string Description { get; set; } = "";
        public string IpAddress { get; set; }= "";
        public string DeviceInfo { get; set; } = "";
        public User User { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}