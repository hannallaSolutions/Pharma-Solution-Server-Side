using System.Text.Json.Serialization;

namespace SearchTool_ServerSide.Dtos.Chat
{
    public sealed class ChatResponse
    {
        public string Reply { get; set; } = string.Empty;

        // Optional: surface token usage later if you want
        // public int? PromptTokens { get; set; }
        // public int? OutputTokens { get; set; }
    }

    public sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    public sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContentResponse? Content { get; set; }
    }

    public sealed class GeminiContentResponse
    {
        [JsonPropertyName("parts")]
        public List<GeminiPartResponse>? Parts { get; set; }
    }

    public sealed class GeminiPartResponse
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

}