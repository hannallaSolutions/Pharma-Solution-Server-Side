using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Dtos;
using SearchTool_ServerSide.Services;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Authorization;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("scripts"),Authorize(Policy ="Admin")]
    public class ScriptsController(ScriptsService service, UserAccessToken userAccessToken) : ControllerBase
    {


        [HttpGet("simple")]
        public async Task<ActionResult<PagedResponse<SimpleScriptDto>>> GetScriptsSimple(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userData = userAccessToken.tokenData();

            var result = await service.GetScriptsSimpleAsync(pageNumber, pageSize,int.Parse(userData.BranchId));
            return Ok(result);
        }
    }
}
