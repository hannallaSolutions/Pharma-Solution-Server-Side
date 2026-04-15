namespace SearchTool_ServerSide.Logging
{
    public class UserLogQueueItem
    {
        public int UserId { get; set; }
        public string? UserEmail { get; set; }
        public DateTime Date { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = "Unknown";
        public string DeviceInfo { get; set; } = string.Empty;
    }
}