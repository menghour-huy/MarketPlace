using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Market.Client.Filters
{
    public class GlobalExceptionFilter : IAsyncExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.ExceptionHandled)
            {
                return Task.CompletedTask;
            }

            _logger.LogError(context.Exception, "Unhandled exception while processing request.");

            context.Result = new RedirectToActionResult("Error", "Home", null);
            context.ExceptionHandled = true;

            return Task.CompletedTask;
        }
    }
}
