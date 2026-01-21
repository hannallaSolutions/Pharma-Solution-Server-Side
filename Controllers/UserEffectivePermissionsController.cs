using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserEffectivePermissionsController : ControllerBase
    {
        private readonly SearchToolDBContext _db;
        public UserEffectivePermissionsController(SearchToolDBContext db) => _db = db;

        [HttpGet("{userId:int}/permissions")]
        public async Task<IActionResult> GetUserPermissions(int userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("User not found.");

            var perms = await _db.RolePermissions
                .Where(rp => rp.Role == user.Role)
                .Include(rp => rp.Permission)
                .OrderBy(rp => rp.Permission.Name)
                .Select(rp => new { rp.PermissionId, rp.Permission.Name, rp.Permission.Description })
                .ToListAsync();

            return Ok(new { Role = user.Role.ToString(), Permissions = perms });
        }
    }
}
