using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController<TController> : ControllerBase
    {
        private IMediator _mediator;
        protected readonly ILogger<TController> _logger;

        protected IMediator Mediator => _mediator ??=
            HttpContext.RequestServices.GetService<IMediator>();

        protected BaseApiController(ILogger<TController> logger)
        {
            _logger = logger;
        }
    }
}
