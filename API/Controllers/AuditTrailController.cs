using Application.AuditTrails;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace API.Controllers
{
    public class AuditTrailController : BaseApiController<AuditTrailController>
    {
        public AuditTrailController(ILogger<AuditTrailController> logger) : base(logger)
        {
        }

        [HttpGet] // api/audittrail
        public async Task<ActionResult<List<AuditTrail>>> GetAuditTrails(
            [FromQuery] string searchTerm, 
            [FromQuery] string userFilter,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 6
            )
        {
            Log.Information("Fetching all audit trails");

            try
            {
                var result = await Mediator.Send(new AuditTrailList.Query
                {
                    SearchTerm = searchTerm,
                    UserFilter = userFilter,
                    StartDate = startDate,
                    EndDate = endDate,
                    Page = page,
                    PageSize = pageSize
                });

                _logger.LogInformation("Successfully retrieved audit trails");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching audit trails");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost] // api/audittrail
        public async Task<IActionResult> Create([FromBody] CreateAuditTrail.Command command)
        {
            await Mediator.Send(command);
            return Ok();
        }
    }
}
