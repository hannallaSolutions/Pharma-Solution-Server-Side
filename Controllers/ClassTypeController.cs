using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;

namespace SearchTool_ServerSide.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ClassTypeController : ControllerBase
    {
        private readonly SearchToolDBContext _context;

        public ClassTypeController(SearchToolDBContext context)
        {
            _context = context;
        }

        [HttpGet("GetAllClassTypes")]
        public async Task<IActionResult> GetAllClassTypes()
        {
            var classTypes = await _context.ClassTypes
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name
                })
                .ToListAsync();

            return Ok(classTypes);
        }
    }
}