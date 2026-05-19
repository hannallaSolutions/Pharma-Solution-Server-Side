using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Dtos.BranchDTOs;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("Branch")]
    public class BranchController : ControllerBase
    {
        private readonly BranchService _branchService;
        private readonly UserAccessToken _userAccessToken;

        public BranchController(
            BranchService branchService,
            UserAccessToken userAccessToken)
        {
            _branchService = branchService;
            _userAccessToken = userAccessToken;
        }

        [HttpGet("GetMainCompanyByBranchId")]
        public async Task<IActionResult> GetMainCompanyByBranchId([FromQuery] int branchId)
        {
            var mainCompany = await _branchService.GetMainCompanyByBranchId(branchId);
            return mainCompany != null ? Ok(mainCompany) : NotFound();
        }

        [HttpGet("GetAllBranchesByMainCompanyId")]
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> GetAllBranchesByMainCompanyId([FromQuery] int mainCompanyId)
        {
            var branches = await _branchService.GetAllBranchesByMainCompanyId(mainCompanyId);
            return Ok(branches);
        }

        [HttpGet("GetAllBranches")]
        public async Task<IActionResult> GetAllBranches()
        {
            var branches = await _branchService.GetAllBranches();
            return Ok(branches);
        }
        
        //        GetAllMainCompanyBranchesByBranchId

        [HttpGet("GetAllMainCompanyBranchesByBranchId")]
        public async Task<IActionResult> GetAllMainCompanyBranchesByBranchId()
        {
            var tokenData = _userAccessToken.tokenData();

            if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.BranchId))
            {
                return NotFound("Invalid Data");
            }
            var branches = await _branchService.GetAllMainCompanyBranchesByBranchId(int.Parse(tokenData.BranchId));

            if (!int.TryParse(tokenData.BranchId, out var branchId))
            {
                return BadRequest("Invalid BranchId");
            }

            _ = await _branchService.GetAllMainCompanyBranchesByBranchId(branchId);
            return Ok(branches);
        }

        [HttpPost("CreateBranch")]
        public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDto branch)
        {
            var createdBranch = await _branchService.CreateAsync(branch);
            return CreatedAtAction(nameof(GetAllBranches), new { id = createdBranch.Id }, createdBranch);
        }

        [HttpDelete("DeleteBranchById")]
        public async Task<IActionResult> DeleteBranchById([FromQuery] int branchId)
        {
            var result = await _branchService.DeleteBranchById(branchId);
            return result ? NoContent() : NotFound();
        }

        [HttpGet("GetBranchesWithUsersCount")]
        public async Task<IActionResult> GetBranchesWithUsersCount()
        {
            var data = await _branchService.GetBranchesWithUsersCount();
            return Ok(data);
        }

        [HttpPut("EditBranch")]
        public async Task<IActionResult> EditBranch(
            [FromQuery] int branchId,
            [FromBody] EditBranchDto branch)
        {
            var result = await _branchService.EditBranch(branchId, branch);
            return result ? NoContent() : NotFound();
        }
    }
}