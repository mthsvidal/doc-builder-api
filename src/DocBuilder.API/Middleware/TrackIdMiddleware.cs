using DocBuilder.Domain.Context;

namespace DocBuilder.API.Middleware;

public class TrackIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string TrackIdHeaderName = "X-Track-Id";

    public TrackIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate or retrieve TrackId from header
        var trackId = context.Request.Headers[TrackIdHeaderName].FirstOrDefault() 
                      ?? Guid.NewGuid().ToString();

        // Set in context for the entire request
        RequestContext.TrackId = trackId;

        // Add to response headers
        context.Response.Headers[TrackIdHeaderName] = trackId;

        try
        {
            await _next(context);
        }
        finally
        {
            // Clear after request completes
            RequestContext.Clear();
        }
    }
}
