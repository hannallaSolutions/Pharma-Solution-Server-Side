using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Dtos;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("scripts")]
    public class ScriptsController : ControllerBase
    {
        private readonly ScriptsService _service;

        public ScriptsController(ScriptsService service)
        {
            _service = service;
        }

        [HttpGet("simple")]
        [Authorize] // شيلها مؤقتًا لو عايز تختبر بدون توكن
        public async Task<ActionResult<PagedResponse<SimpleScriptDto>>> GetScriptsSimple(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetScriptsSimpleAsync(pageNumber, pageSize);
            return Ok(result);
        }
    }
}
