namespace API.Middleware
{
    public class GlobalErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorHandlerMiddleware> _logger;

        public GlobalErrorHandlerMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing the request. TraceId: {TraceId}", context.Items["TraceId"]);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                ArgumentException => StatusCodes.Status400BadRequest, // Bad Request
                InvalidOperationException => StatusCodes.Status409Conflict, // Conflict
                _ => StatusCodes.Status500InternalServerError // Internal Server Error
            };

            context.Response.StatusCode = statusCode;

            var errorResponse = new
            {
                context.Response.StatusCode,
                exception.Message, // Pass the exception message to the frontend
                Details = statusCode == StatusCodes.Status500InternalServerError ? null : exception.StackTrace // Include details only in non-500 errors
            };

            return context.Response.WriteAsJsonAsync(errorResponse);
        }

    }
}
