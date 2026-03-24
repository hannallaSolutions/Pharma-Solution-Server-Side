using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("api/drug-search")]
    public class DrugDecisionPanelController : ControllerBase
    {
        private readonly DrugService _drugService;

        public DrugDecisionPanelController(DrugService drugService)
        {
            _drugService = drugService;
        }

        // GET: /api/drug-search/decision-panel?ndc=...&rxgroup=...&pcn=...&bin=...
        [HttpGet("decision-panel")]
        public async Task<IActionResult> GetDecisionPanel(
            [FromQuery] string ndc,
            [FromQuery] string? rxgroup = null,
            [FromQuery] string? pcn = null,
            [FromQuery] string? bin = null)
        {
            if (string.IsNullOrWhiteSpace(ndc))
                return BadRequest("ndc is required.");

            // 1) Source drug
            var sourceDrug = await _drugService.SearchByNdc(ndc);
            if (sourceDrug == null)
                return NotFound("Drug not found.");

            // 2) Classes
            // NOTE: SearchByNdc return type may include Id — adjust if naming differs
            int drugId = (int)(sourceDrug.GetType().GetProperty("Id")?.GetValue(sourceDrug) ?? 0);
            if (drugId == 0)
                return Ok(new { drug = sourceDrug, alternatives = new object[0], note = "Drug has no id/classes." });

            var classes = await _drugService.GetClassesByDrugId(drugId);

            // MVP: pick first classInfoId
            // classes may be list of class objects; we need classInfoId field name.
            // We'll try to read it dynamically in a safe way.
            int classInfoId = 0;
            foreach (var c in classes as IEnumerable<object> ?? Enumerable.Empty<object>())
            {
                classInfoId =
                    (int?)(c.GetType().GetProperty("ClassInfoId")?.GetValue(c) ??
                           c.GetType().GetProperty("classInfoId")?.GetValue(c) ??
                           c.GetType().GetProperty("Id")?.GetValue(c) ??
                           0) ?? 0;

                if (classInfoId != 0) break;
            }

            if (classInfoId == 0)
            {
                return Ok(new
                {
                    drug = sourceDrug,
                    profit = (object?)null,
                    alternatives = new object[0],
                    note = "No class info found for this drug."
                });
            }

            // 3) Alternatives (filters endpoint already returns list(s) we can use)
            var altResult = await _drugService.GetAlternativesWithInsuranceFilters(
                classInfoId,
                ndc,
                rxgroup,
                pcn,
                bin
            );

            // 4) Return as-is for MVP (front will render top 3)
            return Ok(new
            {
                drug = sourceDrug,
                classInfoId,
                alternatives = altResult
            });
        }
    }
}