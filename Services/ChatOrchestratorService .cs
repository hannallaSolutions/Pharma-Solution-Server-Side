using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;

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
            var reply = await _gemini.SendMessageAsync(message.Trim(), history, ct);

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
                .Where(m => m.ChatId == chatId && m.Show)
                .OrderByDescending(m => m.Timestamp)
                .Take(max)
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatTurn(m.Role, m.Text))
                .ToListAsync(ct);

            return msgs;
        }
    }
}
