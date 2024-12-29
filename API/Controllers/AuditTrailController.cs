using Application.AuditTrails;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers
{
    public class AuditTrailController : BaseApiController<AuditTrailController>
    {
        private readonly DefaultAuditTrailSettings _defaultSettings;

        public AuditTrailController(ILogger<AuditTrailController> logger, IOptions<DefaultAuditTrailSettings> defaultSettings) : base(logger)
        {
        }

        [HttpGet] // api/audittrail
        public async Task<ActionResult<List<AuditTrail>>> GetAuditTrails(
            [FromQuery] string searchTerm, 
            [FromQuery] string userFilter,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? page,
            [FromQuery] int? pageSize
            )
        {

            try
            {
                int resolvedPage = page ?? _defaultSettings.Page;
                int resolvedPageSize = pageSize ?? _defaultSettings.PageSize;

                var result = await Mediator.Send(new AuditTrailList.Query
                {
                    SearchTerm = searchTerm,
                    UserFilter = userFilter,
                    StartDate = startDate,
                    EndDate = endDate,
                    Page = resolvedPage,
                    PageSize = resolvedPageSize
                });

                return Ok(result);
            }
            catch 
            {
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
