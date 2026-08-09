using Market.API.Application.Auth.Commands.Login;
using Market.API.Application.Auth.Commands.Register;
using Market.API.Application.Common.Interfaces;
using Market.Shared.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IIdentityService _identityService;

        public AuthController(IMediator mediator, IIdentityService identityService)
        {
            _mediator = mediator;
            _identityService = identityService;
        }
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

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResult>> Refresh([FromBody] RefreshTokenRequest request)
        {
            var (succeeded, result) = await _identityService.RefreshTokenAsync(request.RefreshToken);
            if (!succeeded || result is null)
            {
                return Unauthorized("Invalid or expired refresh token.");
            }

            return Ok(result);
        }
    }
}
