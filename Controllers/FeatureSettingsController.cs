using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Dtos.FeatureSettingsDTOs;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("FeatureSettings")]
    [AllowAnonymous]
    public class FeatureSettingsController : ControllerBase
    {
        private readonly CompanyFeatureSettingService _service;

        public FeatureSettingsController(CompanyFeatureSettingService service)
        {
            _service = service;
        }

        [HttpGet("Catalog")]
        public IActionResult GetCatalog()
        {
            var catalog = _service.GetCatalog();
            return Ok(catalog);
        }

        [HttpGet("Company/{mainCompanyId}")]
        public async Task<IActionResult> GetCompanySettings(int mainCompanyId)
        {
            var settings = await _service.GetCompanySettingsViewAsync(mainCompanyId);
            return Ok(settings);
        }

        [HttpPut("Company/{mainCompanyId}/Feature/{featureKey}")]
        public async Task<IActionResult> Update(
            int mainCompanyId,
            string featureKey,
            [FromBody] UpdateMainCompanyFeatureSettingDto dto)
        {
            try
            {
                await _service.UpdateAsync(
                    mainCompanyId,
                    featureKey,
                    dto,
                    updatedByUserId: null
                );

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}