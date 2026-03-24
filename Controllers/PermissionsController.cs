using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Dtos.PermissionDtos;

namespace SearchTool_ServerSide.Controllers
{

    [ApiController]
    [Route("permissions")]
    public class PermissionController : ControllerBase
    {
        // define connection to database
        private readonly SearchToolDBContext  _db;
        public PermissionController(SearchToolDBContext db)
        {
            _db = db;
        }

        // get all permissions
        [HttpGet]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _db.Permissions.ToListAsync();

            return Ok(permissions);
        }
        
         // get permission by id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPermissionById(int id)
        {
            var permission = await _db.Permissions.FindAsync(id);
            if( permission == null)
            {
                return NotFound();
            }
            return Ok(permission);
        }

        // create new permission

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePermissionDto dto)
        {
            var name = dto.Name.Trim(); // remove leading/trailing spaces
            
            // make sure the permission name is unique
            var exists = await _db.Permissions.AnyAsync(x => x.Name == name);
            if (exists) return Conflict("Permission name already exists.");

            var p = new Permission
            {
                Name = name,
                Description = dto.Description?.Trim(),
                Url = dto.Url.Trim(),
                HttpMethod = dto.HttpMethod.Trim().ToUpper()
            };

            _db.Permissions.Add(p);
            await _db.SaveChangesAsync();


            return CreatedAtAction(
            nameof(GetPermissionById),
             new { id = p.Id },
             new { p.Id, p.Name, p.Description });
        }

        //edit permission
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermission(int id, [FromBody] CreatePermissionDto dto)
        {
            var permission = await _db.Permissions.FindAsync(id);
            if(permission == null)
            {
                return NotFound("Permission not found.");
            }
               
               // also check for unique name as created above
            var name = dto.Name.Trim();
            var exists = await _db.Permissions.AnyAsync(x => x.Name == name && x.Id != id);
            if (exists) return Conflict("Permission name already exists.");

            permission.Name = name;
            permission.Description = dto.Description;
            permission.Url = dto.Url;
            permission.HttpMethod = dto.HttpMethod;

            await _db.SaveChangesAsync();
            return Ok(permission);

        }

        // delete permission
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var permission = await _db.Permissions.FindAsync(id);
            if(permission == null) return NotFound("Permission not found.");

            _db.Permissions.Remove(permission);
            await _db.SaveChangesAsync();

            // successful deletion
            return Ok("Permission deleted successfully.");
        }
    }
}