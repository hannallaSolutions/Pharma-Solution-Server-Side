
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
public async Task<IActionResult> GetAllMainCompanies()
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



        [HttpPut("{id}")]
        public async Task<IActionResult> EditMainCompany(int id, [FromBody] EditMainCompanyDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Invalid data.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Company name is required.");
            }

            var result = await _mainCompanyService.EditMainCompanyAsync(id, dto);

            if (!result)
            {
                return NotFound($"Main company with id {id} was not found.");
            }

            return Ok(new
            {
                message = "Main company updated successfully."
            });
        }
    

    [HttpDelete("DeleteMainCompany/{id}")]
public async Task<IActionResult> DeleteMainCompany(int id)
{
    var result = await _mainCompanyService.DeleteMainCompanyAsync(id);

    if (!result)
    {
        return NotFound(new
        {
            success = false,
            message = "Main company not found"
        });
    }

    return Ok(new
    {
        success = true,
        message = "Main company deleted successfully"
    });
}
    }
    
}