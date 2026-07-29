using Market.API.Application.Common.Interfaces;
using Market.Shared.Contracts.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IIdentityService _identityService;

        public UserController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetProfile(Guid userId)
        {
            var (succeeded, profile, error) = await _identityService.GetUserProfileAsync(userId);

            if (!succeeded || profile is null)
                return NotFound(new { message = error ?? "User not found." });

            return Ok(profile);
        }

        [HttpPut("{userId:guid}")]
        public async Task<IActionResult> UpdateProfile(Guid userId, [FromBody] UpdateUserProfileRequest request)
        {
            var (succeeded, profile, error) = await _identityService.UpdateUserProfileAsync(userId, request);

            if (!succeeded || profile is null)
                return BadRequest(new { message = error ?? "Failed to update profile." });

            return Ok(profile);
        }
    }
}
