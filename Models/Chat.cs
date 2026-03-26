namespace SearchTool_ServerSide.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool Show { get; set; }
        public bool TempChat { get; set; } = false;
        public List<Message> Messages { get; set; } = new List<Message>();


    }
}