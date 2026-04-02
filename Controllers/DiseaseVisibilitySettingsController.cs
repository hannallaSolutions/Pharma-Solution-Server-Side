using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.DTOs;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Models.Enums;
using SearchTool_ServerSide.Data;
   using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("api/disease-visibility")]
    public class DiseaseVisibilitySettingsController : ControllerBase
    {
        private readonly SearchToolDBContext _db;

        public DiseaseVisibilitySettingsController(SearchToolDBContext db)
        {
            _db = db;
        }

        [HttpGet("settings")]
        public async Task<ActionResult<DiseaseVisibilitySettingsDto>> GetSettings()
        {
            var settings = await _db.DiseaseVisibilitySettings.FirstOrDefaultAsync(x => x.Id == 1);

            // Safety fallback if seed didn’t exist
            if (settings == null)
            {
                settings = new DiseaseVisibilitySettings { Id = 1, Mode = DiseaseVisibilityMode.AllDoctors };
                _db.DiseaseVisibilitySettings.Add(settings);
                await _db.SaveChangesAsync();
            }

            return Ok(new DiseaseVisibilitySettingsDto { Mode = (int)settings.Mode } );//casting the mode to int , customize output with only mode
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] DiseaseVisibilitySettingsDto dto)
        {
            if (!Enum.IsDefined(typeof(DiseaseVisibilityMode), dto.Mode))
                return BadRequest("Invalid mode value. Use 1=AllDoctors, 2=CustomizeByUser, 3=OwnOnly.");

            var settings = await _db.DiseaseVisibilitySettings.FirstOrDefaultAsync(x => x.Id == 1);
            if (settings == null)
            {
                settings = new DiseaseVisibilitySettings { Id = 1 };
                _db.DiseaseVisibilitySettings.Add(settings);
            }

            settings.Mode = (DiseaseVisibilityMode)dto.Mode;
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }


     

        //  returns visible diseases for CURRENT logged-in user
        [Authorize]
        [HttpGet("visible-diseases")]
        public async Task<IActionResult> GetVisibleDiseases()
        {
            // 1) get mode
            var settings = await _db.DiseaseVisibilitySettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == 1);

            var mode = settings?.Mode ?? DiseaseVisibilityMode.AllDoctors;

            // 2) mode 1 => all diseases
            if (mode == DiseaseVisibilityMode.AllDoctors)
            {
                var all = await _db.Diseases.AsNoTracking()
                    .Select(d => new { d.Id, d.Name, d.Description })
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                return Ok(all);
            }

            // 3) mode 2 or 3 => assigned diseases for current user
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized("Missing/invalid user id in token.");

            var diseaseIds = await _db.UserDiseaseVisibility.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.DiseaseId)
                .ToListAsync();

            var visible = await _db.Diseases.AsNoTracking()
                .Where(d => diseaseIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Name, d.Description })
                .OrderBy(d => d.Name)
                .ToListAsync();

            return Ok(visible);
        }
    
    }
}
