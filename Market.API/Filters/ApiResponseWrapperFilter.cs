using Market.API.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Market.API.Filters;

public class ApiResponseWrapperFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is not ObjectResult objectResult)
        {
            await next();
            return;
        }

        if (objectResult.Value?.GetType().IsGenericType == true &&
            objectResult.Value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            await next();
            return;
        }

        var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;
        if (statusCode == 0)
        {
            statusCode = StatusCodes.Status200OK;
        }

        var isSuccess = statusCode is >= 200 and < 300;
        var message = isSuccess ? "Request completed successfully" : "Request failed";

        var errors = isSuccess
            ? Array.Empty<string>()
            : ExtractErrors(objectResult.Value);

        context.Result = new ObjectResult(new ApiResponse<object>
        {
            Success = isSuccess,
            Message = message,
            Data = isSuccess ? objectResult.Value : null,
            Errors = errors
        })
        {
            StatusCode = statusCode
        };

        await next();
    }

    private static IEnumerable<string> ExtractErrors(object? value)
    {
        return value switch
        {
            null => new[] { "An error occurred." },
            string message => new[] { message },
            IEnumerable<string> messages => messages,
            _ => new[] { value.ToString() ?? "An error occurred." }
        };
    }
}
