using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController, Route("Branch")]
    public class BranchController(BranchService _branchService) : ControllerBase
    {
        [HttpGet("GetMainCompanyByBranchId")]
        public async Task<IActionResult> GetMainCompanyByBranchId([FromQuery] int branchId)
        {
            var mainCompany = await _branchService.GetMainCompanyByBranchId(branchId);
            return mainCompany != null ? Ok(mainCompany) : NotFound();
        }
        [HttpGet("GetAllBranchesByMainCompanyId"),Authorize(Policy ="SuperAdmin")]
        public async Task<IActionResult> GetAllBranchesByMainCompanyId([FromQuery] int mainCompanyId)
        {
            var branches = await _branchService.GetAllBranchesByMainCompanyId(mainCompanyId);
            return Ok(branches);
        }
    }
}