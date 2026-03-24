using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Authorization;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("sharePoint")]
   // [HasPermission("sharePoint_access")]
    public class SharePointController : ControllerBase
    {
        [HttpGet("token-test")]
        public IActionResult GetTokenTest()
        {
            return Ok("Token  is valid and working!");
        }
    }
}