namespace SearchTool_ServerSide.Dtos.LogDtos
{
    public class LogsReadDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string DeviceInfo { get; set; }
        public DateTime Date { get; set; }
    }
}