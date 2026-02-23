using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.DTOs;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("api/disease-visibility")]
    public class DiseaseVisibilityAssignmentsController : ControllerBase
    {
        private readonly SearchToolDBContext _db;
        public DiseaseVisibilityAssignmentsController(SearchToolDBContext db) => _db = db;

        // List doctors (Users with Role=Doctor)
        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors()
        {
            var doctors = await _db.Users
                .Where(u => u.Role == Role.Doctor)
                .Select(u => new { u.Id, u.Name, u.Email })
                .OrderBy(u => u.Name)
                .ToListAsync();

            return Ok(doctors);
        }

        // Get assigned diseases for doctor
        [HttpGet("doctors/{userId:int}/diseases")]
        public async Task<IActionResult> GetDoctorDiseases(int userId)
        {
            var diseaseIds = await _db.UserDiseaseVisibility
                .Where(x => x.UserId == userId)
                .Select(x => x.DiseaseId)
                .ToListAsync();

            return Ok(new { diseaseIds });
        }

        // Replace assigned diseases list for doctor
        [HttpPut("doctors/{userId:int}/diseases")]
        public async Task<IActionResult> UpdateDoctorDiseases(int userId, [FromBody] UpdateUserDiseasesDto dto)
        {
            // Optional: validate user is Doctor
            var isDoctor = await _db.Users.AnyAsync(u => u.Id == userId && u.Role == Role.Doctor);
            if (!isDoctor) return BadRequest("User is not a Doctor.");

            // Optional: validate diseases exist
            var existingDiseaseIds = await _db.Diseases
                .Where(d => dto.DiseaseIds.Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync();

            // remove old
            var old = await _db.UserDiseaseVisibility.Where(x => x.UserId == userId).ToListAsync();
            _db.UserDiseaseVisibility.RemoveRange(old);

            // add new
            var newRows = existingDiseaseIds.Distinct().Select(did => new UserDiseaseVisibility
            {
                UserId = userId,
                DiseaseId = did
            });

            await _db.UserDiseaseVisibility.AddRangeAsync(newRows);
            await _db.SaveChangesAsync();

            return NoContent();
        }


     

    }
}
