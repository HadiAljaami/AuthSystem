using AuthSystem.Api.Application.DTOs.Common;

namespace AuthSystem.Api.Infrastructure.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            ApiResponse<object> response;
            int statusCode;

            switch (ex)
            {
                case ArgumentException ae:
                    statusCode = StatusCodes.Status400BadRequest;
                    response = ApiResponse<object>.FailureResponse("ARGUMENT_ERROR", ae.Message);
                    break;

                case KeyNotFoundException knf:
                    statusCode = StatusCodes.Status404NotFound;
                    response = ApiResponse<object>.FailureResponse("NOT_FOUND", knf.Message);
                    break;

                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    response = ApiResponse<object>.FailureResponse("SERVER_ERROR", "حدث خطأ غير متوقع", new { exception = ex.Message });
                    break;
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(response);
        }

    }

}
