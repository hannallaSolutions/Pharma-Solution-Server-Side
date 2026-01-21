
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.PermissionDtos;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Controllers
{
[ApiController]
[Route("api/roles")]
public class RolePermissionsController : ControllerBase
{
    private readonly SearchToolDBContext _db;
    public RolePermissionsController(SearchToolDBContext db) => _db = db;

    [HttpGet]
    public IActionResult GetAllRoles()
        => Ok(Enum.GetNames(typeof(Role)));

    [HttpGet("{role}/permissions")]
    public async Task<IActionResult> GetRolePermissions(string role)
    {
        if (!Enum.TryParse<Role>(role, true, out var parsedRole)) //this means  ignore case for role if it is ADMIN or admin or Admin
            return BadRequest("Invalid role.");        //

        var perms = await _db.RolePermissions
            .Where(rp => rp.Role == parsedRole)
            .OrderBy(rp => rp.Permission.Name)
            .Select(rp => new { rp.PermissionId, rp.Permission.Name, rp.Permission.Description })
            .ToListAsync();

        return Ok(perms);
    }

    [HttpPut("{role}/permissions")]
    public async Task<IActionResult> ReplaceRolePermissions(string role, [FromBody] ReplaceRolePermissionsDto dto)
    {
        if (!Enum.TryParse<Role>(role, true, out var parsedRole))
            return BadRequest("Invalid role.");

        var ids = dto.PermissionIds.Distinct().ToList();

      // means if
        var existingIds = await _db.Permissions
            .Where(p => ids.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        if (existingIds.Count != ids.Count)
        {
            var invalidIds = ids.Except(existingIds).ToList();
            return BadRequest(new { Message = "Invalid PermissionIds.", InvalidIds = invalidIds });
        }

        var old = await _db.RolePermissions.Where(rp => rp.Role == parsedRole).ToListAsync();
        _db.RolePermissions.RemoveRange(old);

        _db.RolePermissions.AddRange(ids.Select(id => new RolePermission
        {
            Role = parsedRole,
            PermissionId = id
        }));

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Role permissions updated successfully." });
    }
}

}

/*
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.PermissionDtos;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolePermissionsController : ControllerBase
    {
        private readonly SearchToolDBContext _db;
        public RolePermissionsController(SearchToolDBContext db)
        {
            _db = db;
        } 

         // Get permissions for a specific role
         [HttpGet("{role}/permissions")]
         public async Task<IActionResult> GetRolePermissions(Role role)
         {

            // fetch permissions for the given role

             var perms = await _db.RolePermissions // access RolePermissions table
             .Where(rp => rp.Role == role)        // filter by role
             .Include(rp => rp.Permission)         // get related Permission details
             .OrderBy(rp => rp.Permission.Name)    // sort by permission name
             .Select(rp => new {rp.PermissionId, rp.Permission.Name, rp.Permission.Description})
                .ToListAsync();

                return Ok(perms);
         }


        // get all roles
        [HttpGet("roles")]
        public IActionResult GetAllRoles()
        {
            var roles = Enum.GetValues(typeof(Role))
                .Cast<Role>()
                .Select(r => new { Id = (int)r, Name = r.ToString() })
                .ToList();

            return Ok(roles);
        }
         

         // get all permissions
        [HttpGet("permissions")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _db.Permissions
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Ok(permissions);
        }
        // Replace ALL permissions for a specific role

        [HttpPut("{role}/permissions")]
        public async Task<IActionResult> ReplaceRolePermissions(Role role, [FromBody] ReplaceRolePermissionsDto dto)
        {
            var ids = dto.PermissionIds.Distinct().ToList();

            var existingIds = await _db.Permissions
                .Where(p => ids.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            if (ids.Count == 0)  return BadRequest("PermissionIds cannot be empty.");

            if (existingIds.Count != ids.Count)
                return BadRequest("One or more PermissionIds are invalid.");


            // remove old
            var old = await _db.RolePermissions.Where(rp => rp.Role == role).ToListAsync();
            _db.RolePermissions.RemoveRange(old);

            // add new
            _db.RolePermissions.AddRange(ids.Select(id => new RolePermission
            {
                Role = role,
                PermissionId = id
            }));

            await _db.SaveChangesAsync();
            return Ok(new { Message = "Role permissions updated successfully." });
        }
    }
}
*/