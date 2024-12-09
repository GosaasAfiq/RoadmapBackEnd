using Application.AuditTrails;
using Application.Roadmaps;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace API.Controllers
{
    public class UsersController : BaseApiController<UsersController>
    {
        public UsersController(ILogger<UsersController> logger) : base(logger)
        {
        }

        [HttpGet] // api/audittrail
        public async Task<ActionResult<List<UserList>>> GetAuditTrails()
        {
            try
            {
                var userList = await Mediator.Send(new UserList.Query());

                _logger.LogInformation("Get user");
                return Ok(userList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching user");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
