using Application.Roadmaps;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace API.Controllers
{
    public class RoadmapsController : BaseApiController<RoadmapsController>
    {
        private readonly DefaultRoadmapSettings _defaultSettings;
        public RoadmapsController(ILogger<RoadmapsController> logger, IOptions<DefaultRoadmapSettings> defaultSettings) : base(logger)
        {
            _defaultSettings = defaultSettings.Value;
        }

        [HttpGet] // api/roadmaps
        public async Task<ActionResult<List<Roadmap>>> GetRoadmaps(
            [FromQuery] string searchTerm, 
            [FromQuery] string filter,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string sortBy
            )

        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier); 

            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized("Invalid UserId claim.");
            }

            filter = filter ?? _defaultSettings.Filter;
            int resolvedPage = page ?? _defaultSettings.Page; 
            int resolvedPageSize = pageSize ?? _defaultSettings.PageSize; 
            sortBy = sortBy ?? _defaultSettings.SortBy;


            var roadmaps = await Mediator.Send(new List.Query
            {
                UserId = userId,
                SearchTerm = searchTerm,
                Filter = filter,
                Page = resolvedPage, 
                PageSize = resolvedPageSize, 
                SortBy = sortBy
            });


            return Ok(roadmaps);
        }

        [HttpGet("{id}")] // api/roadmaps/{id}
        public async Task<ActionResult<Roadmap>> GetRoadmap(Guid id)
        {

            var roadmap = await Mediator.Send(new Details.Query { Id = id });

            if (roadmap == null)
            {
                return NotFound($"Roadmap with ID {id} not found.");
            }

            return Ok(roadmap);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Create.Command command)
        {

            await Mediator.Send(command);
            return Ok(new { message = "Roadmap created successfully" });

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
            await Mediator.Send(command);
            return Ok();
        }

        [HttpPost("updateroadmap")]
        public async Task<IActionResult> UpdateRoadmap([FromBody] UpdateRoadmap.Command command)
        {
            await Mediator.Send(command);
            return Ok(new { message = "Roadmap updated successfully" });
        }

    }
}
