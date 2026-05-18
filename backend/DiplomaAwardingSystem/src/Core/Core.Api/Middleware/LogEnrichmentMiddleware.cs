using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Core.Api.Middleware;

public sealed class LogEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
            context.Request.Headers.Append("X-Correlation-ID", correlationId);
        }

        var userId = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value ?? "Anonymous"
            : "Anonymous";

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
                {
                    context.Response.Headers.Append("X-Correlation-ID", correlationId);
                }

                return Task.CompletedTask;
            });

            await next(context);
        }
    }
}
