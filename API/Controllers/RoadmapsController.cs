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

            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier); // Assuming the claim is stored as NameIdentifier

                // Convert the userId to Guid
                if (!Guid.TryParse(userIdString, out Guid userId))
                {
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
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("{id}")] // api/roadmaps/{id}
        public async Task<ActionResult<Roadmap>> GetRoadmap(Guid id)
        {

            try
            {
                // Only pass the roadmap ID (not userId) to the Details.Query
                var roadmap = await Mediator.Send(new Details.Query { Id = id });

                if (roadmap == null)
                {
                    return NotFound($"Roadmap with ID {id} not found.");
                }

                return Ok(roadmap);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Create.Command command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [HttpPut("updatenode")]
        public async Task<IActionResult> UpdateRoadmap([FromBody] UpdateNode.Command command)
        {
            await Mediator.Send(command);
            return Ok();

        }

        [HttpPut("deleteroadmap")]
        public async Task<IActionResult> DeleteRoadmap([FromBody] DeleteRoadmap.Command command)
        {
            try
            {
                await Mediator.Send(command);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("updateroadmap")]
        public async Task<IActionResult> UpdateRoadmap([FromBody] UpdateRoadmap.Command command)
        {
            await Mediator.Send(command);
            return Ok();
        }

    }
}
