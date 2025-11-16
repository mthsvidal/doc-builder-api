using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using DocBuilder.Domain.Context;
using DocBuilder.Domain.Interfaces.Integrations;
using DocBuilder.Domain.Constants;

namespace DocBuilder.API.Middleware;

public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMinioIntegration _minioIntegration;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ApiLoggingMiddleware(RequestDelegate next, IMinioIntegration minioIntegration)
    {
        _next = next;
        _minioIntegration = minioIntegration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var trackId = RequestContext.TrackId;
        
        // Build path structure: Controller/Method-Endpoint/request or Controller/Method-Endpoint/response
        var pathSegments = context.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? new List<string>();
        
        var controller = pathSegments.Count > 1 ? pathSegments[1] : "root";
        var endpointPath = pathSegments.Count > 2 ? string.Join("-", pathSegments.Skip(2)) : pathSegments.LastOrDefault() ?? "index";
        var endpoint = $"{context.Request.Method.ToLower()}-{endpointPath}";
        
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");

        // Capture request
        var requestBody = await CaptureRequestBodyAsync(context.Request);
        
        object? requestData = null;
        if (!string.IsNullOrWhiteSpace(requestBody))
        {
            try
            {
                requestData = JsonSerializer.Deserialize<object>(requestBody);
            }
            catch
            {
                requestData = requestBody; // Keep as string if not valid JSON
            }
        }

        var requestLog = new
        {
            trackId = trackId,
            timestamp = timestamp,
            method = context.Request.Method,
            path = context.Request.Path.Value,
            queryString = context.Request.QueryString.Value,
            headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            data = requestData
        };

        // Save request to MinIO (fire and forget)
        var requestPath = $"{controller}/{endpoint}/request/{trackId}_{timestamp}.json";
        _ = Task.Run(async () =>
        {
            try
            {
                var requestJson = JsonSerializer.Serialize(requestLog, JsonOptions);
                await _minioIntegration.UploadJsonAsync(StorageConstants.ApiLogsBucketName, requestPath, requestJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TrackId: {trackId}] Error logging request: {ex.Message}");
            }
        });

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log exception details
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            
            var errorResponse = new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace,
                type = ex.GetType().Name
            };
            
            var errorJson = JsonSerializer.Serialize(errorResponse, JsonOptions);
            var errorBytes = Encoding.UTF8.GetBytes(errorJson);
            await responseBodyStream.WriteAsync(errorBytes);
            
            Console.WriteLine($"[TrackId: {trackId}] Exception occurred: {ex.Message}");
        }
        finally
        {
            // Capture response body
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);

            object? responseData = null;
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    responseData = JsonSerializer.Deserialize<object>(responseBody);
                }
                catch
                {
                    responseData = responseBody; // Keep as string if not valid JSON
                }
            }

            var responseLog = new
            {
                trackId = trackId,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff"),
                statusCode = context.Response.StatusCode,
                headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                data = responseData
            };

            // Save response to MinIO (fire and forget)
            var responsePath = $"{controller}/{endpoint}/response/{trackId}_{timestamp}.json";
            _ = Task.Run(async () =>
            {
                try
                {
                    var responseJson = JsonSerializer.Serialize(responseLog, JsonOptions);
                    await _minioIntegration.UploadJsonAsync(StorageConstants.ApiLogsBucketName, responsePath, responseJson);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"[TrackId: {trackId}] Error logging response: {logEx.Message}");
                }
            });

            // Copy response back to original stream
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private static async Task<string> CaptureRequestBodyAsync(HttpRequest request)
    {
        if (request.Body == null)
            return string.Empty;

        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }
}
