using Market.API.Application.Auth.Commands.Login;
using Market.API.Application.Auth.Commands.Register;
using Market.API.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AuthController(IMediator mediator) => _mediator = mediator;
        [HttpPost("register")]
        public async Task<ActionResult<Guid>> Register(RegisterCommand command)
        {
            var userId = await _mediator.Send(command);
            return Ok(new { userId });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResult>> Login(LoginCommand command)
        {
            var result = await _mediator.Send(command);
            if (result is null)
                return Unauthorized("Invalid email or password");

            return Ok(result);
        }
    }
}
