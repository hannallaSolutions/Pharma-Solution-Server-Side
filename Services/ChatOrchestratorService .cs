using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;
using System.Text.Json;

namespace SearchTool_ServerSide.Services
{
    public interface IChatOrchestratorService
    {
        // Send message (create chat if null) + store both sides + return reply
        Task<(int chatId, string reply)> ChatAsync(int? chatId, int userId, string message, CancellationToken ct);

        // NEW: list previous chats for user (for widget list)
        Task<List<ChatListItemDto>> GetUserChatsAsync(int userId, int take, CancellationToken ct);

        // NEW: load full chat messages (for widget open)
        Task<ChatDetailsDto?> GetChatDetailsAsync(int chatId, int userId, CancellationToken ct);

        // NEW: suggest follow-up questions based on user chat history
        Task<List<string>> GetSuggestedQuestionsAsync(int? chatId, int userId, int take, CancellationToken ct);
    }

    public sealed record ChatListItemDto(
        int Id,
        DateTime UpdatedAt,
        string? LastMessage
    );

    public sealed record ChatMessageDto(
        string Role,     // "user" | "model"
        string Text,
        DateTime Timestamp
    );

    public sealed record ChatDetailsDto(
        int ChatId,
        List<ChatMessageDto> Messages
    );

    public class ChatOrchestratorService : IChatOrchestratorService
    {
        private readonly SearchToolDBContext _db;
        private readonly IGeminiChatService _gemini;
        private readonly ILogger<ChatOrchestratorService> _logger;

        public ChatOrchestratorService(
            SearchToolDBContext db,
            IGeminiChatService gemini,
            ILogger<ChatOrchestratorService> logger)
        {
            _db = db;
            _gemini = gemini;
            _logger = logger;
        }

        public async Task<(int chatId, string reply)> ChatAsync(int? chatId, int userId, string message, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(message))
                return (chatId ?? 0, "Please enter a message.");

            // 1) Ensure chat exists AND belongs to this user
            var actualChatId = await EnsureChatAsync(chatId, userId, ct);

            // 2) Save user message
            await AddMessageAsync(actualChatId, "user", message.Trim(), ct);

            // 3) Load history from DB
            var history = await LoadHistoryAsync(actualChatId, GeminiChatService.MaxMessageContext, ct);

            // 4) Call Gemini
            var reply = await _gemini.SendMessageAsync(message.Trim(), history);

            // 5) Save model reply
            await AddMessageAsync(actualChatId, "model", reply, ct);

            return (actualChatId, reply);
        }

        // ===================== NEW: List Chats =====================
        public async Task<List<ChatListItemDto>> GetUserChatsAsync(int userId, int take, CancellationToken ct)
        {
            // Build a projection with scalar fields first so EF Core can translate ordering
            var projected = _db.Chats
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.Show)
                .Select(c => new
                {
                    c.Id,
                    UpdatedAt = _db.Messages
                        .Where(m => m.ChatId == c.Id && m.Show)
                        .Max(m => (DateTime?)m.Timestamp) ?? DateTime.MinValue,
                    LastMessage = _db.Messages
                        .Where(m => m.ChatId == c.Id && m.Show)
                        .OrderByDescending(m => m.Timestamp)
                        .Select(m => m.Text)
                        .FirstOrDefault()
                });

            var list = await projected
                .OrderByDescending(x => x.UpdatedAt)
                .Take(take)
                .Select(x => new ChatListItemDto(x.Id, x.UpdatedAt, x.LastMessage))
                .ToListAsync(ct);

            return list;
        }

        // ===================== NEW: Load Chat Messages =====================
        public async Task<ChatDetailsDto?> GetChatDetailsAsync(int chatId, int userId, CancellationToken ct)
        {
            // Ensure ownership
            var chatExists = await _db.Chats
                .AsNoTracking()
                .AnyAsync(c => c.Id == chatId && c.UserId == userId && c.Show, ct);

            if (!chatExists)
                return null;

            var msgs = await _db.Messages
                .AsNoTracking()
                .Where(m => m.ChatId == chatId && m.Show)
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatMessageDto(
                    m.Role,       // "user" | "model"
                    m.Text,
                    m.Timestamp
                ))
                .ToListAsync(ct);

            return new ChatDetailsDto(chatId, msgs);
        }

        // ===================== NEW: Suggest follow-up questions =====================
        public async Task<List<string>> GetSuggestedQuestionsAsync(int? chatId, int userId, int take, CancellationToken ct)
        {
            take = Math.Clamp(take, 1, 10);

            int actualChatId;
            if (chatId.HasValue && chatId.Value > 0)
            {
                // validate ownership via EnsureChatAsync (throws if not found)
                actualChatId = await EnsureChatAsync(chatId, userId, ct);
            }
            else
            {
                actualChatId = await GetLatestChatIdAsync(userId, ct);
                if (actualChatId <= 0)
                    return new List<string>();
            }

            var history = await LoadHistoryAsync(actualChatId, GeminiChatService.MaxMessageContext, ct);

var systemPrompt = $@"
You are an expert medical assistant.

Task:
Generate exactly {take} suggested questions that the user might ask in a NEW medical chat.

Guidelines:
- Do NOT continue the previous conversation.
- Do NOT reference any specific symptoms, drugs, diagnoses, or cases mentioned earlier.
- Use the conversation history only to understand the user's general interests and question patterns.
- The questions must sound like realistic first questions in a brand-new medical conversation.
- Make the questions diverse and useful.
- Each question must be independent and different from the others.
- Do NOT mention previous chats or context.

Output rules:
Output must be a JSON array of strings only, for example: [""Question 1"", ""Question 2""]
Do not add any additional text.
";

            var response = await _gemini.SendMessageAsync($"Please suggest {take} follow-up questions.", history, systemPrompt, ct);
            return ParseSuggestedQuestions(response, take);
        }

        private async Task<int> GetLatestChatIdAsync(int userId, CancellationToken ct)
        {
            return await _db.Chats
                .Where(c => c.UserId == userId && c.Show)
                .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.Timestamp) ?? DateTime.MinValue)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(ct);
        }

        private List<string> ParseSuggestedQuestions(string response, int maxCount)
        {
            if (string.IsNullOrWhiteSpace(response))
                return new List<string>();

            // Try to parse as JSON array first
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(response);
                if (parsed != null && parsed.Count > 0)
                    return parsed.Take(maxCount).Select(q => q?.Trim()).Where(q => !string.IsNullOrEmpty(q)).ToList();
            }
            catch
            {
                // Ignore parse errors and fall back to line splitting
            }

            // Fall back: split by newlines and strip numbering/bullets
            var lines = response
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Select(l =>
                {
                    // Remove leading numbering like "1) ", "- " or "• "
                    var idx = l.IndexOfAny(new[] { ')', '.', ' ' });
                    if (idx > 0 && int.TryParse(l.Substring(0, idx).TrimEnd('.', ')'), out _))
                        return l.Substring(idx + 1).Trim();
                    if (l.StartsWith("- "))
                        return l.Substring(2).Trim();
                    if (l.StartsWith("• "))
                        return l.Substring(2).Trim();
                    return l;
                })
                .Where(l => !string.IsNullOrEmpty(l))
                .Take(maxCount)
                .ToList();

            return lines;
        }

        // ===================== Helpers =====================

        private async Task<int> EnsureChatAsync(int? chatId, int userId, CancellationToken ct)
        {
            if (chatId.HasValue && chatId.Value > 0)
            {
                // IMPORTANT: also check UserId so user can’t open someone else’s chat
                var exists = await _db.Chats.AnyAsync(
                    c => c.Id == chatId.Value && c.UserId == userId && c.Show,
                    ct);

                if (!exists)
                    throw new InvalidOperationException("ChatId not found (or not yours).");

                return chatId.Value;
            }

            var chat = new Chat
            {
                UserId = userId,
                Show = true,
                TempChat = false
            };

            _db.Chats.Add(chat);
            await _db.SaveChangesAsync(ct);
            return chat.Id;
        }

        private async Task AddMessageAsync(int chatId, string role, string text, CancellationToken ct)
        {
            var safeRole = role is "user" or "model" ? role : "user";

            _db.Messages.Add(new Message
            {
                ChatId = chatId,
                Role = safeRole,
                Text = text,
                Timestamp = DateTime.UtcNow,
                Show = true
            });

            await _db.SaveChangesAsync(ct);
        }

        private async Task<List<ChatTurn>> LoadHistoryAsync(int chatId, int max, CancellationToken ct)
        {
            var msgs = await _db.Messages
                .AsNoTracking()
                .Where(m => m.ChatId == chatId && m.Show && m.Role == "user")
                .OrderByDescending(m => m.Timestamp)
                .Take(max)
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatTurn(m.Role, m.Text))
                .ToListAsync(ct);

            return msgs;
        }
    }
}
