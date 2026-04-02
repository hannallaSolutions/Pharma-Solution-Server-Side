using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Dtos.DiseaseDtos;
using SearchTool_ServerSide.Services;
using SearchTool_ServerSide.Authorization;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("Disease"), Authorize(Policy = "Pharmacist")]
    public class DiseaseController : ControllerBase
    {
        private readonly DiseaseService _diseaseService;
        private readonly UserAccessToken _userAccessToken;

        public DiseaseController(DiseaseService diseaseService, UserAccessToken userAccessToken)
        {
            _diseaseService = diseaseService;
            _userAccessToken = userAccessToken;
        }

        [HttpGet("GetByName/{name}")]
        public async Task<IActionResult> GetDiseaseByName(string name)
        {
            var disease = await _diseaseService.GetDiseaseByName(name);
            if (disease == null) return NotFound();
            return Ok(disease);
        }

        [HttpGet("GetAllVisible")]
        public async Task<IActionResult> GetAllVisible()
        {
            var token = _userAccessToken.tokenData();
            var userId = int.TryParse(token.UserId, out var id) ? id : 0;

            var diseases = await _diseaseService.GetVisibleDiseasesAsync(userId);
            return Ok(diseases);
        }

        [HttpGet("SearchByDisease")]
        public async Task<IActionResult> SearchByDisease([FromQuery] string name)
        {
            var disease = await _diseaseService.SearchByDisease(name);
            return Ok(disease);
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllDiseases()
        {
            var diseases = await _diseaseService.GetAllDiseases();
            return Ok(diseases);
        }

        [HttpPost("Add"), Authorize(Policy = "Doctor")]
        public async Task<IActionResult> AddDisease([FromBody] DiseaseAddDto diseaseAddDto)
        {
            var disease = await _diseaseService.AddDisease(diseaseAddDto);
            return Ok(disease);
        }

        [HttpGet("GetDrugInteractions/{diseaseName}")]
        public async Task<IActionResult> GetDrugInteractions(
            [FromQuery] string diseaseName,
            [FromQuery] int pageSize = 10,
            [FromQuery] int pageNumber = 1)
        {
            var interactions = await _diseaseService.GetDrugInteractions(diseaseName, pageSize, pageNumber);
            return Ok(interactions);
        }

        [HttpPost("AddDrugDisease"), Authorize]
        [HasPermission("AddDrugToDisease")]
        public async Task<IActionResult> AddDrugDisease([FromBody] DrugDiseaseHistoryAddDto dto)
        {
            var user = _userAccessToken.tokenData();
            dto.UserId = int.TryParse(user.UserId, out var userId) ? userId : dto.UserId;

            var drugDisease = await _diseaseService.AddDrugDiseaseHistory(dto);
            return Ok(drugDisease);
        }

        [HttpGet("GetDrugDiseaseHistory"), Authorize]
        public async Task<IActionResult> GetDrugDiseaseHistory()
        {
            var userEmail = _userAccessToken.tokenData().Email;
            var history = await _diseaseService.GetDrugDiseaseHistory(userEmail);
            return Ok(history);
        }

        [HttpGet("SoftDeleteDrugDiseaseHistory/{id}"), Authorize(Policy = "Doctor")]
        public async Task<IActionResult> SoftDeleteDrugDiseaseHistory(int id)
        {
            var result = await _diseaseService.SoftDeleteDrugDiseaseHistory(id);
            if (!result) return NotFound();
            return Ok();
        }
    }
}
