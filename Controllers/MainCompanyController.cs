
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Dtos.MainCompanyDtos;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Services;
using SearchTool_ServerSide.Data;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Authorization;

namespace SearchTool_ServerSide.Controllers
{

[ApiController]
[Route("MainCompany")]


        public class MainCompanyController : ControllerBase
    {

        private readonly MainCompanyService _mainCompanyService;
        private readonly SearchToolDBContext _context;

        public MainCompanyController(MainCompanyService mainCompanyService, SearchToolDBContext context)
        {
            _mainCompanyService = mainCompanyService;
            _context = context;
        }

        [HttpGet("GetAllMainCompanies")]
     //   [HasPermission("GetAllMainCompanies")]
        public async Task<IActionResult> GetAllMainCompaniesAsync()
        {
            var companies = await _mainCompanyService.GetAllMainCompaniesAsync();
            return Ok(companies);
        }

        [HttpGet("GetMainCompanyById/{id}")]
        public async Task<IActionResult> GetMainCompanyByIdAsync(int id)
        {
            var company = await _mainCompanyService.GetMainCompanyByIdAsync(id);
            return company != null ? Ok(company) : NotFound();
        }

        
        [HttpPost("AddMainCompany")]
      //  [HasPermission("GetAllMainCompanies")]
        public async Task<IActionResult> AddMainCompanyAsync([FromBody]MainCompanyAddDto mainCompanyDto)
        {
            if (mainCompanyDto == null || string.IsNullOrWhiteSpace(mainCompanyDto.Name) || mainCompanyDto.SpecialtyId <= 0)
            {
                return BadRequest("Invalid company data.");
            }

            var addedCompany = await _mainCompanyService.AddMainCompanyAsync(mainCompanyDto);
            return Ok(addedCompany);
        }
  

        //GET /MainCompany/GetMainCompaniesWithBranchesCount
        [HttpGet("GetMainCompaniesWithBranchesCount")]
        public async Task<IActionResult> GetMainCompaniesWithBranchesCount()
        {
            var data = await  _context.MainCompanies
            .Select( m => new
            {
                mainCompanyId = m.Id,
                name = m.Name ,
                branchesCount = _context.Branches.Count( b => b.MainCompanyId == m.Id)
            }

            )
            .ToListAsync();
            return Ok(data);
        }
    }
    
}