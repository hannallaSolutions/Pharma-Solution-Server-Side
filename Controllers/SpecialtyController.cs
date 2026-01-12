using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Dtos.SpecialtyDTOs;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Services;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Helpers;

namespace SearchTool_ServerSide.Controllers
{

[ApiController]
[Route("api/[controller]")]

public class SpecialtyController : ControllerBase
{
    //clarify our db connection
    private readonly SearchToolDBContext _Context;

    public SpecialtyController(SearchToolDBContext context)
    {
        _Context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSpecialty([FromBody] CreateSpecialtyDTOs specialtyDto)
    {
         if (string.IsNullOrWhiteSpace(specialtyDto.Name))
             return BadRequest(new ApiResponse<string> (false,"Specialty name cannot be empty.", null ));

             var exist = await _Context.Specialties.AnyAsync(s => s.Name.ToLower() == specialtyDto.Name.ToLower());

            if (exist)
            return Conflict(new ApiResponse<string> (false, "Specialty with the same name already exists.", null ));

            var  specialty = new Specialty { Name = specialtyDto.Name  };
            _Context.Specialties.Add(specialty);

            await _Context.SaveChangesAsync();

            return Ok(new ApiResponse<SpecialtyDTOs> (true, "Specialty created successfully.",
             new SpecialtyDTOs { Id = specialty.Id, Name = specialty.Name } ));

    }

    [HttpGet]
    public async Task<IActionResult> GetAllSpecialties()
    {
        var specialties = await _Context.Specialties
            .Select(s => new SpecialtyDTOs
{
    Id = s.Id,
    Name = s.Name
})

            .ToListAsync();

        return Ok(new ApiResponse<List<SpecialtyDTOs>>(true, "Specialties retrieved successfully.",
         specialties));

    }


     // endpoint for edit 
     [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSpecialty(int id, [FromBody] UpdateSpecialtyDTOs specialtyDto)
    {
        var specialty = await _Context.Specialties.FindAsync(id);
        if (specialty == null)
            return NotFound(new ApiResponse<string>(false, "Specialty not found.", null));

        if (string.IsNullOrWhiteSpace(specialtyDto.Name))
            return BadRequest(new ApiResponse<string>(false, "Specialty name cannot be empty.", null));

        var exists = await _Context.Specialties
            .AnyAsync(s => s.Id != id && s.Name.ToLower() == specialtyDto.Name.ToLower());

        if (exists)
            return Conflict(new ApiResponse<string>(false, "Specialty with the same name already exists.", null));

        specialty.Name = specialtyDto.Name;
        await _Context.SaveChangesAsync();

        return Ok(new ApiResponse<SpecialtyDTOs>(true, "Specialty updated successfully.",
            new SpecialtyDTOs { Id = specialty.Id, Name = specialty.Name }));
    }
    // for delete by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSpecialtyById(int id)
    {
        var specialty = await _Context.Specialties.FindAsync(id);

        if (specialty == null)
            return NotFound(new ApiResponse<string>(false, "Specialty not found.", null));

        var specialtyDto = new SpecialtyDTOs
        {
            Id = specialty.Id,
            Name = specialty.Name
        };

        return Ok(new ApiResponse<SpecialtyDTOs>(true, "Specialty retrieved successfully.", specialtyDto));

}

}

}