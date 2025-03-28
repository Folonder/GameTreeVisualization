using System.Text;
using Microsoft.IO;

namespace GameTreeVisualization.Web.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log the request
            await LogRequest(context);

            // Capture the response
            var originalBodyStream = context.Response.Body;
            using var responseBody = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;

            try
            {
                // Call the next middleware
                await _next(context);

                // Log the response
                await LogResponse(context, responseBody, originalBodyStream);
            }
            finally
            {
                // Restore the original response body
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task LogRequest(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering();

                var requestId = Guid.NewGuid().ToString();
                context.Items["RequestId"] = requestId;
                
                var builder = new StringBuilder();
                builder.AppendLine($"---- HTTP Request [{requestId}] {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")} ----");
                builder.AppendLine($"{context.Request.Method} {context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}");
                builder.AppendLine($"Connection: {context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}");
                
                // Log request headers
                builder.AppendLine("Headers:");
                foreach (var header in context.Request.Headers)
                {
                    builder.AppendLine($"  {header.Key}: {header.Value}");
                }

                // Log request body
                if (context.Request.ContentLength > 0)
                {
                    builder.AppendLine("Body:");
                    using var reader = new StreamReader(
                        context.Request.Body,
                        encoding: Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        leaveOpen: true);
                    
                    var body = await reader.ReadToEndAsync();
                    builder.AppendLine(body);
                    
                    // Reset the request body position
                    context.Request.Body.Position = 0;
                }

                _logger.LogInformation(builder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging request");
            }
        }

        private async Task LogResponse(HttpContext context, MemoryStream responseBody, Stream originalBodyStream)
        {
            try
            {
                var requestId = context.Items["RequestId"] as string ?? "unknown";
                
                var builder = new StringBuilder();
                builder.AppendLine($"---- HTTP Response [{requestId}] {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")} ----");
                builder.AppendLine($"Status Code: {context.Response.StatusCode}");
                
                // Log response headers
                builder.AppendLine("Headers:");
                foreach (var header in context.Response.Headers)
                {
                    builder.AppendLine($"  {header.Key}: {header.Value}");
                }

                // Log response body
                responseBody.Position = 0;
                var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
                if (!string.IsNullOrEmpty(responseContent))
                {
                    // Truncate very large responses
                    var truncated = false;
                    var maxLength = 10000; // 10K characters max
                    if (responseContent.Length > maxLength)
                    {
                        responseContent = responseContent.Substring(0, maxLength);
                        truncated = true;
                    }

                    builder.AppendLine("Body:");
                    builder.AppendLine(responseContent);
                    
                    if (truncated)
                    {
                        builder.AppendLine("... (response truncated)");
                    }
                }

                _logger.LogInformation(builder.ToString());

                // Copy the response body to the original stream
                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging response");
            }
        }
    }

    // Extension method to make registration easier
    public static class RequestResponseLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
        }
    }
}