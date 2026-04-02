using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Dtos.EmailDTOs;
using SearchTool_ServerSide.Services;

[ApiController]
[Route("api/[controller]")]
public class EmailCampaignController : ControllerBase
{
    private readonly EmailCampaignService _service;

    public EmailCampaignController(EmailCampaignService service)
    {
        _service = service;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromForm] BulkEmailRequestDto request)
    {
        if (request.RecipientsFile == null)
            return BadRequest("Recipients file required");

        if (string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest("Subject required");

        if (string.IsNullOrWhiteSpace(request.Body))
            return BadRequest("Body required");

        var result = await _service.SendBulkEmailAsync(request);
        return Ok(result);
    }
}