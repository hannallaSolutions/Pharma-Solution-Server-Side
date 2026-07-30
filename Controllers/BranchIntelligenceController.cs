using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Dtos.BranchIntelligenceDto;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("api/dashboard/branch-intelligence"), Authorize]
    public class BranchIntelligenceController(BranchIntelligenceService branchIntelligenceService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(BranchIntelligenceOverviewDto), 200)]
        public async Task<IActionResult> GetOverview(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] bool allBranches,
            CancellationToken ct)
        {
            var data = await branchIntelligenceService.GetOverviewAsync(dateFrom, dateTo, allBranches, ct);
            return Ok(new
            {
                data,
                meta = new
                {
                    branchCount = data.Leaderboard.Count,
                    generatedAt = DateTime.UtcNow
                }
            });
        }

        [HttpGet("{branchId:long}")]
        [ProducesResponseType(typeof(BranchDetailDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBranchDetail(
            long branchId,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            CancellationToken ct)
        {
            var (status, data) = await branchIntelligenceService.GetBranchDetailAsync(branchId, dateFrom, dateTo, ct);

            switch (status)
            {
                case BranchDetailAccessStatus.NotFound:
                    return NotFound(new { message = "Branch not found." });
                case BranchDetailAccessStatus.Forbidden:
                    return StatusCode(403, new { message = "You are not authorized to view this branch." });
                default:
                    return Ok(new
                    {
                        data,
                        meta = new
                        {
                            generatedAt = DateTime.UtcNow
                        }
                    });
            }
        }
    }
}
