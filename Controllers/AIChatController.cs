using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIChatController : ControllerBase
    {
        private readonly IChatOrchestratorService _orchestrator;
        private readonly ILogger<AIChatController> _logger;
        private readonly UserAccessToken _userAccessToken;

        public AIChatController(
            IChatOrchestratorService orchestrator,
            ILogger<AIChatController> logger,
            UserAccessToken userAccessToken)
        {
            _orchestrator = orchestrator;
            _logger = logger;
            _userAccessToken = userAccessToken;
        }

        public sealed class ChatRequest
        {
            public int? ChatId { get; set; }          // null => new chat
            public string Message { get; set; } = ""; // required
        }

        public sealed class ChatResponse
        {
            public int ChatId { get; set; }
            public string Reply { get; set; } = "";
        }

        // ===================== Helpers =====================
        private bool TryGetUserId(out int userId, out IActionResult? errorResult)
        {
            userId = 0;
            errorResult = null;

            var userData = _userAccessToken.tokenData();
            if (userData == null || string.IsNullOrWhiteSpace(userData.UserId))
            {
                errorResult = Unauthorized("Invalid or missing token data");
                return false;
            }

            if (!int.TryParse(userData.UserId, out userId) || userId <= 0)
            {
                errorResult = Unauthorized("Invalid user id in token data");
                return false;
            }

            return true;
        }

        // ===================== POST: Send message =====================
        // POST /api/AIChat/Chat
        [HttpPost("Chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
        {
            try
            {
                if (request == null)
                    return BadRequest("Invalid request.");

                if (string.IsNullOrWhiteSpace(request.Message))
                    return BadRequest("Message cannot be empty.");

                if (!TryGetUserId(out var userId, out var error))
                    return error!;

                var (chatId, reply) = await _orchestrator.ChatAsync(
                    request.ChatId,
                    userId,
                    request.Message.Trim(),
                    ct
                );

                return Ok(new ChatResponse
                {
                    ChatId = chatId,
                    Reply = reply
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Chat endpoint");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ===================== GET: List previous chats =====================
        // GET /api/AIChat/Chats?take=30
        [HttpGet("Chats")]
        public async Task<IActionResult> GetChats([FromQuery] int take = 30, CancellationToken ct = default)
        {
            try
            {
                if (!TryGetUserId(out var userId, out var error))
                    return error!;

                take = Math.Clamp(take, 1, 100);

                var chats = await _orchestrator.GetUserChatsAsync(userId, take, ct);

                // camelCase for frontend
                return Ok(chats.Select(c => new
                {
                    id = c.Id,
                    updatedAt = c.UpdatedAt,
                    lastMessage = c.LastMessage
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetChats endpoint");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ===================== GET: Load one chat messages =====================
        // GET /api/AIChat/Chat/{chatId}
        [HttpGet("Chat/{chatId:int}")]
        public async Task<IActionResult> GetChat(int chatId, CancellationToken ct = default)
        {
            try
            {
                if (chatId <= 0)
                    return BadRequest("Invalid chatId.");

                if (!TryGetUserId(out var userId, out var error))
                    return error!;

                var chat = await _orchestrator.GetChatDetailsAsync(chatId, userId, ct);

                if (chat == null)
                    return NotFound(new { error = "Chat not found." });

                return Ok(new
                {
                    chatId = chat.ChatId,
                    messages = chat.Messages.Select(m => new
                    {
                        role = m.Role,          // "user" | "model"
                        text = m.Text,
                        timestamp = m.Timestamp
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetChat endpoint");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
