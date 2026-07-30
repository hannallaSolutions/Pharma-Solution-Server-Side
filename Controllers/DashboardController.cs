using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("api/dashboard"), Authorize]
    public class DashboardController(DashboardAnalyticsService analyticsService) : ControllerBase
    {
        [HttpGet("scripts")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetScripts(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? branchId,
            [FromQuery] bool allBranches,
            CancellationToken ct)
        {
            var data = await analyticsService.GetScriptsAsync(dateFrom, dateTo, branchId, allBranches, ct);
            return Ok(new
            {
                data,
                meta = new
                {
                    count = data.Count,
                    generatedAt = DateTime.UtcNow
                }
            });
        }
    }
}
