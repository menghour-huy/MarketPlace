using Microsoft.AspNetCore.Http;

namespace Market.Client.Middleware
{
    /// <summary>
    /// Middleware to populate ViewBag with user session data on every request.
    /// This ensures user information (like profile picture) is available in views.
    /// </summary>
    public class UserSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public UserSessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only populate ViewBag for authenticated requests
            if (context.User?.Identity?.IsAuthenticated ?? false)
            {
                // Get user data from session
                var userImageUrl = context.Session.GetString("UserImageUrl");
                var userFullName = context.Session.GetString("UserFullName");

                // Store in HttpContext.Items for access in controllers/views
                if (!string.IsNullOrEmpty(userImageUrl))
                {
                    context.Items["UserImageUrl"] = userImageUrl;
                }

                if (!string.IsNullOrEmpty(userFullName))
                {
                    context.Items["UserFullName"] = userFullName;
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method to register UserSessionMiddleware
    /// </summary>
    public static class UserSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserSession(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserSessionMiddleware>();
        }
    }
}
