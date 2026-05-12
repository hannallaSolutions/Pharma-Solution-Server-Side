using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Dtos.DrugWholesalerPrescriberDtos;
using SearchTool_ServerSide.Services;

namespace SearchTool_ServerSide.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DrugWholesalerPrescriberController(DrugWholesalerPrescriberService _service) : ControllerBase
    {


        // =====================================================
        // Upload Excel or CSV file
        // POST: api/DrugWholesalerPrescriber/import
        // =====================================================
        [HttpPost("import")]
        public async Task<IActionResult> ImportPricesFile(
            [FromForm] IFormFile file,
            [FromForm] int defaultPrescriberId,
            CancellationToken ct)
        {
            try
            {
                var result = await _service.ImportPricesFileAsync(
                    file,
                    defaultPrescriberId,
                    ct);

                return Ok(new
                {
                    message = "Import completed.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while importing wholesaler prices.",
                    error = ex.Message
                });
            }
        }

        // =====================================================
        // Add one price manually
        // POST: api/DrugWholesalerPrescriber
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> AddSingle(
            [FromBody] AddDrugWholesalerPrescriberDto dto,
            CancellationToken ct)
        {
            try
            {
                var result = await _service.AddSingleAsync(
                    dto.DrugId,
                    dto.WholesalerId,
                    dto.PrescriberId,
                    dto.Price,
                    dto.PriceDate,
                    dto.AWP,
                    dto.WAC,
                    dto.ASP,
                    dto.MAC,
                    dto.BillingUnit,
                    dto.DrugClass,
                    dto.QuarterYear,
                    dto.SourceFileName,
                    dto.SourcePath,
                    ct);

                return Ok(new
                {
                    message = "Price added successfully.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while adding price.",
                    error = ex.Message
                });
            }
        }

        // =====================================================
        // Get latest prices for drug and prescriber
        // GET: api/DrugWholesalerPrescriber/latest?drugId=1&prescriberId=2
        // =====================================================
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestPricesForDrug(
            [FromQuery] int drugId,
            [FromQuery] int prescriberId,
            CancellationToken ct)
        {
            try
            {
                var result = await _service.GetLatestPricesForDrugAsync(
                    drugId,
                    prescriberId,
                    ct);

                return Ok(new
                {
                    message = "Latest prices loaded successfully.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while loading latest prices.",
                    error = ex.Message
                });
            }
        }

        // =====================================================
        // Get best price for drug and prescriber
        // GET: api/DrugWholesalerPrescriber/best?drugId=1&prescriberId=2
        // =====================================================
        [HttpGet("best")]
        public async Task<IActionResult> GetBestPrice(
            [FromQuery] int drugId,
            [FromQuery] int prescriberId,
            CancellationToken ct)
        {
            try
            {
                var result = await _service.GetBestPriceAsync(
                    drugId,
                    prescriberId,
                    ct);

                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "No wholesaler price found for this drug and prescriber."
                    });
                }

                return Ok(new
                {
                    message = "Best price loaded successfully.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while loading best price.",
                    error = ex.Message
                });
            }
        }
    }
}