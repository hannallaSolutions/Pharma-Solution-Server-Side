using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Dtos.BranchDTOs;
using SearchTool_ServerSide.Models;
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

        // get all branches
        [HttpGet("GetAllBranches")]
        public async Task<IActionResult> GetAllBranches()
        {
            var branches = await _branchService.GetAllBranches();
            return Ok(branches);
        }

        // create new branch
        [HttpPost("CreateBranch")]
        public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDto branch)
        {
            var createdBranch = await _branchService.CreateAsync(branch);
            return CreatedAtAction(nameof(GetAllBranches), new { id = createdBranch.Id }, createdBranch);
        }

        // delete branch by id
        [HttpDelete("DeleteBranchById")]
        public async Task<IActionResult> DeleteBranchById([FromQuery] int branchId)
        {
            var result = await _branchService.DeleteBranchById(branchId);
            return result ? NoContent() : NotFound();
        }

        
    }
}