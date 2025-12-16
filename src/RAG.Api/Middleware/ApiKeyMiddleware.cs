using System.Security.Cryptography;

namespace RAG.Api.Middleware;

/// <summary>
/// Middleware that validates API key authentication.
/// Checks for X-API-KEY header and compares against configured value.
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-API-KEY";
    private readonly RequestDelegate _next;
    private readonly string _apiKey;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(
        RequestDelegate next, 
        IConfiguration configuration,
        ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        
        // Read API key from environment variable first, then config
        _apiKey = Environment.GetEnvironmentVariable("API_KEY") 
            ?? configuration["ApiKey"] 
            ?? string.Empty;
        
        // Validate that API key is configured when middleware is registered
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException(
                "API Key authentication middleware is registered but no API key is configured. " +
                "Set API_KEY environment variable or ApiKey in configuration.");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for Swagger endpoints
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        // Skip authentication for health check endpoints (if any)
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Check for API key header
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            _logger.LogWarning("API request rejected: Missing {HeaderName} header from {RemoteIp}", 
                ApiKeyHeaderName, context.Connection.RemoteIpAddress);
            
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "Unauthorized", 
                message = "API Key is missing. Include X-API-KEY header in your request." 
            });
            return;
        }

        // Validate API key (constant-time comparison to prevent timing attacks)
        if (!CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(extractedApiKey!),
            System.Text.Encoding.UTF8.GetBytes(_apiKey)))
        {
            _logger.LogWarning("API request rejected: Invalid API key from {RemoteIp}", 
                context.Connection.RemoteIpAddress);
            
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "Unauthorized", 
                message = "Invalid API Key." 
            });
            return;
        }

        _logger.LogDebug("API key validated successfully for request to {Path}", context.Request.Path);
        await _next(context);
    }
}
