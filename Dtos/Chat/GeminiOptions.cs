namespace SearchTool_ServerSide.Dtos.Chat
{
    public sealed class GeminiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.5-flash";
        public double Temperature { get; set; } = 0.4;
        public int MaxOutputTokens { get; set; } = 512;
    }
}