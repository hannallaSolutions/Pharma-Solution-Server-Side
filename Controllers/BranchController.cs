using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Dtos.BranchDTOs;
using SearchTool_ServerSide.Services;
using SearchTool_ServerSide.Data;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("Branch")]
    public class BranchController : ControllerBase
    {
        private readonly BranchService _branchService;
        private readonly SearchToolDBContext _context;

        public BranchController(BranchService branchService, SearchToolDBContext context)
        {
            _branchService = branchService;
            _context = context;
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
            var data = await _context.Branches
                .Select(b => new
                {
                    branchId = b.Id,
                    branchName = b.Name,
                    usersCount = _context.Users.Count(u => u.BranchId == b.Id)
                     // get users where user in branch row = user in matching row in users table
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}
