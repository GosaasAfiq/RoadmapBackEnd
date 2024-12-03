using Application.Roadmaps;
using Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace API.Controllers
{
    public class RoadmapsController : BaseApiController<RoadmapsController>
    {

        public RoadmapsController(ILogger<RoadmapsController> logger) : base(logger)
        {
        }

        [HttpGet] // api/roadmaps
        public async Task<ActionResult<List<Roadmap>>> GetRoadmaps()
        {
            Log.Information("Fetching all roadmaps");

            try
            {
                var roadmaps = await Mediator.Send(new List.Query());
                _logger.LogInformation("Successfully retrieved {Count} roadmaps", roadmaps.Count);

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
    }
}
