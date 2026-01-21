using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Dtos.DiseaseDtos;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("Disease"),Authorize(Policy = "Pharmacist")]
    public class DiseaseController(DiseaseService diseaseService,UserAccessToken userAccessToken) : ControllerBase
    {
        [HttpGet("GetByName/{name}")]
        public async Task<IActionResult> GetDiseaseByName(string name)
        {
            var disease = await diseaseService.GetDiseaseByName(name);
            if (disease == null)
            {
                return NotFound();
            }
            return Ok(disease);
        }
        [HttpGet("SearchByDisease")]
       public async Task<IActionResult> SearchByDisease([FromQuery] string name)
        {
            var disease =  await diseaseService.SearchByDisease(name);

            return Ok(disease);
        }
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllDiseases()
        {
            var diseases = await diseaseService.GetAllDiseases();
            return Ok(diseases);
        }
        [HttpPost("Add"),Authorize(Policy = "Doctor")]
        public async Task<IActionResult> AddDisease([FromBody] DiseaseAddDto diseaseAddDto)
        {
            var disease = await diseaseService.AddDisease(diseaseAddDto);
            return Ok(disease);
        }
        [HttpGet("GetDrugInteractions/{diseaseName}")]
        public async Task<IActionResult> GetDrugInteractions([FromQuery] string diseaseName, [FromQuery] int pageSize = 10, [FromQuery] int pageNumber = 1)
        {
            var interactions = await diseaseService.GetDrugInteractions(diseaseName, pageSize, pageNumber);
            return Ok(interactions);
        }
        [HttpPost("AddDrugDisease"),Authorize(Policy = "Doctor")]
        public async Task<IActionResult> AddDrugDisease([FromBody] DrugDiseaseHistoryAddDto drugDiseaseHistoryAddDto)
        {
            var user = userAccessToken.tokenData();
            drugDiseaseHistoryAddDto.UserId = int.TryParse(user.UserId, out var userId) ? userId : 103;
            var drugDisease = await diseaseService.AddDrugDiseaseHistory(drugDiseaseHistoryAddDto);
            return Ok(drugDisease);
        }
        [HttpGet("GetDrugDiseaseHistory"), Authorize(Policy = "Doctor")]
        public async Task<IActionResult> GetDrugDiseaseHistory()
        {
            var userEmail = userAccessToken.tokenData().Email;
            var history = await diseaseService.GetDrugDiseaseHistory(userEmail);
            return Ok(history);
        }
        [HttpGet("SoftDeleteDrugDiseaseHistory/{id}"),Authorize(Policy = "Doctor")]
        public async Task<IActionResult> SoftDeleteDrugDiseaseHistory(int id)
        {
            var result = await diseaseService.SoftDeleteDrugDiseaseHistory(id);
            if (!result)
            {
                return NotFound();
            }
            return Ok();
        }
    }
}