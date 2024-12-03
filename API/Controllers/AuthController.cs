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
            _logger.LogInformation("Received Google login request with credential.");

            try
            {
                var response = await _mediator.Send(new Login.Command { Credential = request.Credential });
                _logger.LogInformation("User logged in successfully: {Email}", response.Email);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google login.");
                return BadRequest(new { error = "Invalid token", details = ex.Message });
            }
        }
    }
}
