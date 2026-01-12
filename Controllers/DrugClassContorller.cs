using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("drugClass"),Authorize(Policy = "Pharmacist")]
    public class DrugClassContorller : ControllerBase
    {
        private readonly DrugClassService _drugClassService;
        private readonly UserAccessToken _userAccessToken;

        public DrugClassContorller(DrugClassService drugClassService, UserAccessToken userAccessToken)
        {
            _drugClassService = drugClassService;
            _userAccessToken = userAccessToken;
        }

        [HttpPost("reportStatus")]
        public async Task<IActionResult> ReportStatus([FromBody] DrugClassReportStatusRequest request, CancellationToken ct = default)
        {
            var user = _userAccessToken.tokenData();

            await _drugClassService.ReportStatus(request, user.Email, ct);
            return Ok(new { message = "Status reported successfully." });
        }
        [HttpGet("getReportsByKey")]
        public async Task<IActionResult> GetReportsByKey([FromQuery] string sourceDrugNDC, [FromQuery] string targetDrugNDC, [FromQuery] int classInfoId, CancellationToken ct = default, [FromQuery] int pageSize = 3)
        {
            var reports = await _drugClassService.GetReportsAsyncByKey(sourceDrugNDC, targetDrugNDC, classInfoId, ct, pageSize);
            return Ok(reports);
        }

    }
    public class DrugClassReportStatusRequest
    {
        public string SourceDrugNDC { get; set; }
        public string TargetDrugNDC { get; set; }
        public int ClassInfoId { get; set; }
        public string Status { get; set; } // Expected values: "Approved", "Rejected", or null/empty
    }
}