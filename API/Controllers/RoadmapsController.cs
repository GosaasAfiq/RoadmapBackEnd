using Application.Roadmaps;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;
using System.Security.Claims;

namespace API.Controllers
{
    public class RoadmapsController : BaseApiController<RoadmapsController>
    {
        public RoadmapsController(ILogger<RoadmapsController> logger) : base(logger)
        {
        }

        [HttpGet] // api/roadmaps
        public async Task<ActionResult<List<Roadmap>>> GetRoadmaps(
            [FromQuery] string searchTerm, 
            [FromQuery] string filter = "all", 
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 6)
        {
            Log.Information("Fetching all roadmaps");

            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier); // Assuming the claim is stored as NameIdentifier

                // Convert the userId to Guid
                if (!Guid.TryParse(userIdString, out Guid userId))
                {
                    _logger.LogWarning("Invalid UserId claim: {UserId}", userIdString);
                    return Unauthorized("Invalid UserId claim.");
                }

                var roadmaps = await Mediator.Send(new List.Query
                {
                    UserId = userId,
                    SearchTerm = searchTerm,
                    Filter = filter,
                    Page = page,
                    PageSize = pageSize
                });


                return Ok(roadmaps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all roadmaps");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("{id}")] // api/roadmaps/{id}
        public async Task<ActionResult<Roadmap>> GetRoadmap(Guid id)
        {
            _logger.LogInformation("Fetching roadmap with ID: {Id}", id);

            try
            {
                // Only pass the roadmap ID (not userId) to the Details.Query
                var roadmap = await Mediator.Send(new Details.Query { Id = id });

                if (roadmap == null)
                {
                    _logger.LogWarning("Roadmap with ID {Id} not found", id);
                    return NotFound($"Roadmap with ID {id} not found.");
                }

                _logger.LogInformation("Successfully retrieved roadmap with ID: {Id}", id);
                return Ok(roadmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching roadmap with ID: {Id}", id);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Create.Command command)
        {
            await Mediator.Send(command);
            return Ok();
        }

    }
}
