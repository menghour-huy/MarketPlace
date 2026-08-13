using Market.API.Application.Common.Interfaces;
using Market.Shared.Contracts.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Market.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly ILogger<UserController> _logger;

        public UserController(IIdentityService identityService, ILogger<UserController> logger)
        {
            _identityService = identityService;
            _logger = logger;
        }

        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetProfile(Guid userId)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetProfile request received for userId: {UserId}", userId);

            var (succeeded, profile, error) = await _identityService.GetUserProfileAsync(userId);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Slow GetProfile for userId {UserId}. Total request time {ElapsedMs}ms.",
                    userId,
                    stopwatch.ElapsedMilliseconds);
            }

            if (!succeeded || profile is null)
            {
                _logger.LogWarning("GetProfile failed for userId {UserId}: {Error}", userId, error);
                return NotFound(new { message = error ?? "User not found." });
            }

            _logger.LogInformation("GetProfile succeeded for userId {UserId} in {ElapsedMs}ms", userId, stopwatch.ElapsedMilliseconds);
            return Ok(profile);
        }

        [HttpPut("{userId:guid}")]
        public async Task<IActionResult> UpdateProfile(Guid userId, [FromBody] UpdateUserProfileRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("UpdateProfile request received for userId: {UserId}", userId);

            var (succeeded, profile, error) = await _identityService.UpdateUserProfileAsync(userId, request);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Slow UpdateProfile for userId {UserId}. Total request time {ElapsedMs}ms.",
                    userId,
                    stopwatch.ElapsedMilliseconds);
            }

            if (!succeeded || profile is null)
            {
                _logger.LogWarning("UpdateProfile failed for userId {UserId}: {Error}", userId, error);
                return BadRequest(new { message = error ?? "Failed to update profile." });
            }

            _logger.LogInformation("UpdateProfile succeeded for userId {UserId} in {ElapsedMs}ms", userId, stopwatch.ElapsedMilliseconds);
            return Ok(profile);
        }
    }
}
