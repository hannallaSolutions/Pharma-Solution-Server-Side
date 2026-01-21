/*
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Dtos.PermissionDtos;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("user-permissions")]
    public class UserPermissionController : ControllerBase
    {
        private readonly SearchToolDBContext _db;
        public UserPermissionController(SearchToolDBContext db)  // dependency injection
        {
            _db = db;
        }

        // get all permissions for a specific user
        [HttpGet("users/{userId}/permissions")]
        public async Task<IActionResult> GetUserPermissions(int userId)
        {

            // fetch user with permissions where user id matches
            var user = await _db.Users // access Users table
                .Include(u => u.UserPermissions)  // include UserPermissions navigation property
                .ThenInclude(up => up.Permission)  // include Permission navigation property
                .FirstOrDefaultAsync(u => u.Id == userId); // where u in Users table has Id == userId in permissions table

            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            // extract permissions where user has permissions
            var permissions = user.UserPermissions.Select(up => up.Permission).ToList();
            return Ok(permissions);
        }


        // Replace ALL permissions for the user in one shot  
        [HttpPut("users/{userId}/permissions")]
        public async Task<IActionResult> ReplaceUserPermissions(int userId, [FromBody] ReplaceUserPermissionsDto dto)
        {

            // make sure user exists
            var user = await _db.Users
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId); // where u in Users table has Id == userId in permissions table

            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }
                 
                 // remove duplicates ids from dto
            var requistedIds = dto.PermissionIds.Distinct().ToList();
             
             // Validate that all provided PermissionIds exist in the Permissions table
            var validPermissions = await _db.Permissions
                .Where(p => requistedIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();
                   
                   // if counts don't match, some ids are invalid
                if (validPermissions.Count != requistedIds.Count)
                {
                    var invalidIds = requistedIds.Except(validPermissions);
                    return BadRequest($"The following Permission IDs are invalid: {string.Join(", ", invalidIds)}");
                }

                            // remove old permissions related to the user
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

*/