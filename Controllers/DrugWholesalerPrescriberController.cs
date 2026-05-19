using Microsoft.AspNetCore.Mvc;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Dtos.DrugWholesalerPrescriberDtos;
using SearchTool_ServerSide.Repository;
using SearchTool_ServerSide.Services;
using System.Security.Claims;

namespace SearchTool_ServerSide.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DrugWholesalerPrescriberController(DrugWholesalerPrescriberService _service, UserAccessToken userAccessToken) : ControllerBase
    {
        /*
        [HttpPost("contracts")]
        public async Task<IActionResult> AddContract(
        [FromBody] AddUserInsuranceContractRequest request)
        {
            try
            {
                var contract = await _service.AddContractAsync(request);
                return Ok(contract);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
*/

        [HttpPost("contracts")]
        public async Task<IActionResult> AddContract(
            [FromBody] AddUserInsuranceContractRequest request)
        {
            try
            {
                var userId =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    User.FindFirst("nameid")?.Value;

                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest("Invalid user data.");

                request.UserId = int.Parse(userId);

                var contract = await _service.AddContractAsync(request);
                return Ok(contract);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("reimbursement-parameters")]
        public async Task<IActionResult> GetReimbursementParameters([FromQuery] int insuranceRxId)
        {
            var tokenData = userAccessToken.tokenData();
            if (tokenData == null || tokenData.UserId == null)
            {
                return BadRequest("Invalide Data");
            }
            var result = await _service.GetReimbursementParametersAsync(int.Parse(tokenData.UserId), insuranceRxId);

            if (result == null)
                return NotFound("No active contract found for this user and insurance plan.");

            return Ok(result);
        }
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
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    fullError = ex.ToString()
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


        //to get all data for a prescriber
        [HttpGet("by-prescriber")]
        public async Task<IActionResult> GetAllPricesForPrescriber(
        [FromQuery] int prescriberId,
        CancellationToken ct)
        {
            try
            {
                var result = await _service.GetAllPricesForPrescriberAsync(
                    prescriberId,
                    ct);

                return Ok(new
                {
                    message = "Wholesaler prices loaded successfully.",
                    data = result.Select(x => new
                    {
                        id = x.Id,
                        drugId = x.DrugId,
                        drugName = x.Drug != null ? x.Drug.Name : null,
                        ndc = x.Drug != null ? x.Drug.NDC : null,

                        wholesalerId = x.WholesalerId,
                        wholesalerName = x.Wholesaler != null ? x.Wholesaler.Name : null,

                        prescriberId = x.PrescriberId,
                        prescriberName = x.Prescriber != null ? x.Prescriber.Name : null,

                        price = x.Price,
                        priceDate = x.PriceDate,

                        awp = x.AWP,
                        wac = x.WAC,
                        asp = x.ASP,
                        mac = x.MAC,

                        billingUnit = x.BillingUnit,
                        drugClass = x.DrugClass,
                        quarterYear = x.QuarterYear,

                        sourceFileName = x.SourceFileName,
                        sourcePath = x.SourcePath,

                        createdAt = x.CreatedAt
                    })
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
                    message = "An error occurred while loading wholesaler prices.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }


        //get prescriber options for dropdown
        [HttpGet("prescriber-options")]
        public async Task<IActionResult> GetPrescriberOptions(CancellationToken ct)
        {
            try
            {
                var result = await _service.GetAllPrescribersAsync(ct);

                return Ok(new
                {
                    message = "Prescriber options loaded successfully.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while loading prescriber options.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }


        [HttpGet("my-prices")]
        public async Task<IActionResult> GetMyWholesalerPrices(CancellationToken ct)
        {
            var tokenData = userAccessToken.tokenData();

            if (tokenData == null || tokenData.UserId == null)
                return BadRequest("Invalid user data.");

            var userId = int.Parse(tokenData.UserId);

            var result = await _service.GetAllPricesForPrescriberAsync(userId, ct);

            return Ok(new
            {
                message = "My wholesaler prices loaded successfully.",
                data = result.Select(x => new
                {
                    id = x.Id,
                    drugId = x.DrugId,
                    drugName = x.Drug != null ? x.Drug.Name : null,
                    ndc = x.Drug != null ? x.Drug.NDC : null,

                    wholesalerId = x.WholesalerId,
                    wholesalerName = x.Wholesaler != null ? x.Wholesaler.Name : null,

                    prescriberId = x.PrescriberId,
                    prescriberName = x.Prescriber != null ? x.Prescriber.Name : null,

                    price = x.Price,
                    priceDate = x.PriceDate,

                    awp = x.AWP,
                    wac = x.WAC,
                    asp = x.ASP,
                    mac = x.MAC,

                    billingUnit = x.BillingUnit,
                    drugClass = x.DrugClass,
                    quarterYear = x.QuarterYear,

                    sourceFileName = x.SourceFileName,
                    sourcePath = x.SourcePath,

                    createdAt = x.CreatedAt
                })
            });
        }
        [HttpGet("GetDrugPriceWithInsuranceAsync")]
        public async Task<IActionResult> GetDrugPriceWithInsuranceAsync([FromQuery] int drugId, [FromQuery] int insureanceRxId)
        {
            var tokenData = userAccessToken.tokenData();
            if (tokenData == null || tokenData.BranchId == null)
            {
                return BadRequest("Invalide Data");
            }
            var result = await _service.GetDrugPriceWithInsuranceAsync(drugId, int.Parse(tokenData.BranchId), insureanceRxId);
            return Ok(result);
        }
    }
}