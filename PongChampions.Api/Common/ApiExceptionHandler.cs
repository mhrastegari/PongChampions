using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PongChampions.Api.Common;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problems,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken token)
    {
        var status = exception switch
        {
            ApiException api => api.StatusCode,
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => 409,
            _ => 500
        };

        if (status == 500)
            logger.LogError(exception, "Request failed ({TraceId})", context.TraceIdentifier);

        context.Response.StatusCode = status;

        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status == 500
                    ? "An unexpected server error occurred."
                    : exception is ApiException ? exception.Message : "The resource already exists.",
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            }
        });
    }
}
