namespace SearchTool_ServerSide.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Role { get; set; } = default!; // "user" | "model"
        public string Text { get; set; } = default!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool Show { get; set; } = true;

        public int ChatId { get; set; }
        public Chat Chat { get; set; } = default!;
    }
}