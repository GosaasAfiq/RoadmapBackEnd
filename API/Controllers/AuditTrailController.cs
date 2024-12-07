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
        public async Task<ActionResult<List<AuditTrail>>> GetAuditTrails([FromQuery] string searchTerm)
        {
            Log.Information("Fetching all audit trails");

            try
            {
                var auditTrails = await Mediator.Send(new AuditTrailList.Query
                {
                    SearchTerm = searchTerm
                });

                _logger.LogInformation("Successfully retrieved {Count} audit trails", auditTrails.Count);
                return Ok(auditTrails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching audit trails");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
