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
            var userList = await Mediator.Send(new UserList.Query());
            return Ok(userList);
        }
    }
}
