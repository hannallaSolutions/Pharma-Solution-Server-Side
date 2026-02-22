using Microsoft.Extensions.Options;
using SearchTool_ServerSide.Dtos.Chat;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SearchTool_ServerSide.Services
{
    public interface IGeminiChatService
    {
        Task<string> SendMessageAsync(string message, IReadOnlyList<ChatTurn> history = null, CancellationToken ct = default);
    }
    public sealed record ChatTurn(string Role, string Text); // Role: "user" | "model"

    public sealed class GeminiChatService : IGeminiChatService
    {
        public const int MaxMessageContext = 20;

        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiChatService> _logger;
        private readonly JsonSerializerOptions _json;

        private const string NonMedicalRejection =
            "I can only provide information about medical and healthcare topics. Please ask a medical-related question.";

        public GeminiChatService(
            HttpClient httpClient,
            IOptions<GeminiOptions> options,
            ILogger<GeminiChatService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            _json = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // You can set BaseAddress in DI; fallback here is OK.
            if (_httpClient.BaseAddress == null)
                _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> SendMessageAsync(string message, IReadOnlyList<ChatTurn> history = null, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                    return "Please enter a message.";

                if (string.IsNullOrWhiteSpace(_options.ApiKey))
                    throw new InvalidOperationException("Gemini API key is not configured");

                if (string.IsNullOrWhiteSpace(_options.Model))
                    throw new InvalidOperationException("Gemini model is not configured");

                var systemPrompt = $@"
You are a clinical decision-support assistant for licensed Doctors using a symptoms → disease portal.
Provide concise, high-yield, evidence-based clinical guidance in Markdown.

STRICT RULES:
1) Only answer medical/healthcare questions or related clinical topics or drugs.
2) If NOT medical-related, reply with EXACTLY this and nothing else:
   ""{NonMedicalRejection}""
3) Do NOT give a definitive diagnosis. Provide a ranked differential and how to confirm/rule out.
4) If emergency pattern is suspected, clearly say **Urgency: ED-now**.

Brevity rules (MANDATORY):
- Keep the whole answer short: ~150–300 words (unless user asks “more detail”).
- Default differential: TOP 3 (max 5 only if needed).
- Ask max 3–5 clarifying questions.
- Workup: max 3–5 items.
- Use bullets, no long paragraphs.

NDC rule:
- Do NOT fabricate NDC codes.
- If you cannot confirm exact NDC(s), write EXACTLY:
  ""NDC varies by manufacturer/package; confirm in DailyMed / FDA label or your local formulary.""

Drug details:
- Only include drug dosing/interactions if the user explicitly asks about a drug OR asks for treatment options.

OUTPUT (MARKDOWN) — always follow exactly:

### Triage
- **Urgency:** (Non-urgent / Same-day / ED-now)
- **Why:** 1 line.

### Top differential (ranked)
1) **Dx1** — 1 line why + 1 key test
2) **Dx2** — 1 line why + 1 key test
3) **Dx3** — 1 line why + 1 key test
(Optional 4–5 only if needed.)

### Red flags
- 3–6 bullets max.

### Clarifying questions
- 3–5 bullets max.

### Next steps
- 3–5 bullets max (focused exam + key labs/imaging).

### References
- 2–4 bullets max (FDA label/DailyMed, CDC/WHO, NICE/IDSA/AHA/ACC/ADA as relevant).
";

                var contents = new List<GeminiContent>();

                // 1) Add previous turns
                if (history != null)
                {
                    foreach (var turn in history)
                    {
                        if (string.IsNullOrWhiteSpace(turn?.Text)) continue;

                        // Gemini expects role "user" or "model"
                        var role = turn.Role is "user" or "model" ? turn.Role : "user";

                        contents.Add(new GeminiContent
                        {
                            Role = role,
                            Parts = new List<GeminiPart> { new GeminiPart { Text = turn.Text } }
                        });
                    }
                }
                if (history.Count > MaxMessageContext)
                    history = history.Skip(history.Count - MaxMessageContext).ToList();
                // 2) Add the new user message at the end
                contents.Add(new GeminiContent
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new GeminiPart { Text = message } }
                });
                var requestBody = new GeminiGenerateContentRequest
                {
                    SystemInstruction = new GeminiSystemInstruction
                    {
                        Parts = new List<GeminiPart> { new GeminiPart { Text = systemPrompt } }
                    },
                    Contents = contents,
                    GenerationConfig = new GeminiGenerationConfig
                    {
                        Temperature = _options.Temperature,
                        MaxOutputTokens = _options.MaxOutputTokens // optional but recommended
                    }
                };


                using var httpReq = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"v1beta/models/{_options.Model}:generateContent");

                // Put key in header (better than query string)
                httpReq.Headers.Add("x-goog-api-key", _options.ApiKey);

                var jsonBody = JsonSerializer.Serialize(requestBody, _json);
                httpReq.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(httpReq, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Gemini API error ({(int)response.StatusCode}): {raw}");

                var gemini = JsonSerializer.Deserialize<GeminiResponse>(raw, _json);

                var text =
                    gemini?.Candidates?
                        .FirstOrDefault()?
                        .Content?
                        .Parts?
                        .FirstOrDefault()?
                        .Text;

                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("Invalid response from Gemini API");

                // Enforce non-medical rejection strictly (if model tries to add extra text)
                if (IsNonMedicalRejection(text))
                    return NonMedicalRejection;

                // Optional: light cleanup
                return text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                throw;
            }
        }

        private static bool IsNonMedicalRejection(string text)
        {
            var cleaned = text.Trim().Trim('"');

            if (string.Equals(cleaned, NonMedicalRejection, StringComparison.OrdinalIgnoreCase))
                return true;

            // If it returned the message plus blank lines only
            var lines = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return lines.Length == 1 && string.Equals(lines[0], NonMedicalRejection, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ===== Gemini DTOs =====

    public sealed class GeminiGenerateContentRequest
    {
        [JsonPropertyName("systemInstruction")]
        public GeminiSystemInstruction? SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    public sealed class GeminiSystemInstruction
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    public sealed class GeminiContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user"; // user | model

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    public sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int? MaxOutputTokens { get; set; }
    }


}
