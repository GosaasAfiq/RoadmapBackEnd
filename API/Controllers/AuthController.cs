using Application.Users;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.Extensions.Logging;
using Application.Dto;

namespace API.Controllers
{
    public class AuthController : BaseApiController<AuthController>
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator, ILogger<AuthController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost("google-response")]
        public async Task<IActionResult> GoogleResponse([FromBody] CredentialRequest request)
        {
            var response = await _mediator.Send(new Login.Command { Credential = request.Credential });
            return Ok(response);
        }
    }
}
