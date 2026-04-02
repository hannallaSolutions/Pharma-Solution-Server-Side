using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Dtos.Chat
{
    public sealed class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public IReadOnlyList<ChatTurn> History { get; set; } = Array.Empty<ChatTurn>();
        // Optional: if you want to keep multi-turn history later
        // public List<ChatTurn>? History { get; set; }
    }

}