using Microsoft.AspNetCore.Http;

namespace SearchTool_ServerSide.Dtos.EmailDTOs
{
    public class BulkEmailRequestDto
    {
        public IFormFile RecipientsFile { get; set; } = default!;

        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        public IFormFile? Attachment { get; set; }
    }
}