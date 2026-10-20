using Claims.Application;
using Claims.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            ClaimNotFoundException => (StatusCodes.Status404NotFound, "Claim not found"),
            InvalidClaimTransitionException => (StatusCodes.Status409Conflict, "Invalid claim transition"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (0, null)
        };

        // Anything unmapped falls through to the default handler (500).
        if (status == 0)
            return false;

        logger.LogWarning(exception, "Request failed with {Status}: {Message}", status, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
