using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Dtos.PermissionDtos;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("permissions")]
    public class UserPermissionController : ControllerBase
    {
        private readonly SearchToolDBContext _db;
        public UserPermissionController(SearchToolDBContext db)
        {
            _db = db;
        }

        // get all permissions for a specific user
        [HttpGet("users/{userId}/permissions")]
        public async Task<IActionResult> GetUserPermissions(int userId)
        {
            var user = await _db.Users
                .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            var permissions = user.UserPermissions.Select(up => up.Permission).ToList();
            return Ok(permissions);
        }

        
        // Replace ALL permissions for the user in one shot  
        [HttpPut("users/{userId}/permissions")]
        public async Task<IActionResult> ReplaceUserPermissions(int userId, [FromBody] ReplaceUserPermissionsDto dto)
        {
            var user = await _db.Users
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId); // where u in Users table has Id == userId in permissions table

            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            var requistedIds = dto.PermissionIds.Distinct().ToList();
             
             // Validate that all provided PermissionIds exist
            var validPermissions = await _db.Permissions
                .Where(p => requistedIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

                if (validPermissions.Count != requistedIds.Count)
                {
                    var invalidIds = requistedIds.Except(validPermissions);
                    return BadRequest($"The following Permission IDs are invalid: {string.Join(", ", invalidIds)}");
                }

                            // remove old
            var existingUserPermissions = _db.UserPermissions.Where(up => up.UserId == userId);
            _db.UserPermissions.RemoveRange(existingUserPermissions);   

            // add new
            var newUserPermissions = requistedIds.Select(pid => new UserPermission
            {
                UserId = userId,
                PermissionId = pid
            });
            await _db.UserPermissions.AddRangeAsync(newUserPermissions);
            await _db.SaveChangesAsync();
            return Ok("User permissions updated successfully.");



        }
    }
}