using System.Net;
using System.Text.Json;
using POS.Application.Common.Dto;
using POS.Application.Exceptions;
using System.Linq; 
//// file this get message error and convert message error no understand to we understand it ans is json message 
namespace POS.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>();

            switch (exception)
            {
                case ValidationException validationException: 
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    var allErrors = validationException.Errors
                        .SelectMany(e => e.Value)
                        .ToList();
                    
                    var errorMessage = allErrors.Any() 
                        ? string.Join(" | ", allErrors) 
                        : "Validation failed";
                    
                    response.Success = false;
                    response.Message = errorMessage;
                    response.Data = new { Errors = validationException.Errors }; 
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Success = false;
                    response.Message = _env.IsDevelopment() ? exception.Message : "An internal server error occurred";
                    break;
            }

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }
}