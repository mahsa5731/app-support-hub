using System.Diagnostics;

namespace AppSupportHub.Web.Operations;

public sealed partial class RequestCorrelationMiddleware(
    RequestDelegate next,
    ILogger<RequestCorrelationMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = Guid.TryParse(
            context.Request.Headers[HeaderName].ToString(),
            out Guid supplied)
            ? supplied.ToString("N")
            : Guid.NewGuid().ToString("N");
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
        });
        long startedAt = Stopwatch.GetTimestamp();
        int statusCode = StatusCodes.Status500InternalServerError;
        try
        {
            await next(context);
            statusCode = context.Response.StatusCode;
        }
        finally
        {
            double elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            RequestCompleted(
                logger,
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                statusCode,
                elapsedMilliseconds);
        }
    }

    [LoggerMessage(
        7001,
        LogLevel.Information,
        "HTTP {Method} {Path} completed {StatusCode} in {ElapsedMilliseconds} ms.")]
    private static partial void RequestCompleted(
        ILogger logger,
        string method,
        string path,
        int statusCode,
        double elapsedMilliseconds);
}
