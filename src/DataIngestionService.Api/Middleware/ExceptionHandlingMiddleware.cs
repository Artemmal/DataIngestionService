using DataIngestionService.Api.Exceptions;
using DataIngestionService.Api.Models.Responses;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace DataIngestionService.Api.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
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
            catch (ValidationException exception)
            {
                await HandleValidationExceptionAsync(context, exception);
            }
            catch (DuplicateTransactionException exception)
            {
                await HandleDuplicateTransactionExceptionAsync(context, exception);
            }
            catch (Exception exception)
            {
                await HandleUnexpectedExceptionAsync(context, exception);
            }
        }

        private static async Task HandleValidationExceptionAsync(
            HttpContext context,
            ValidationException exception)
        {
            var response = new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "One or more validation errors occurred.",
                Errors = exception.Errors
                    .Select(error => new ValidationErrorResponse
                    {
                        Field = error.PropertyName,
                        Message = error.ErrorMessage
                    })
                    .ToList()
            };

            await WriteResponseAsync(context, HttpStatusCode.BadRequest, response);
        }

        private static async Task HandleDuplicateTransactionExceptionAsync(
            HttpContext context,
            DuplicateTransactionException exception)
        {
            var response = new ErrorResponse
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate transaction",
                Detail = exception.Message
            };

            await WriteResponseAsync(context, HttpStatusCode.Conflict, response);
        }

        private async Task HandleUnexpectedExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            _logger.LogError(exception, "An unexpected error occurred.");

            var response = new ErrorResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Detail = "An unexpected error occurred while processing the request."
            };

            await WriteResponseAsync(context, HttpStatusCode.InternalServerError, response);
        }

        private static async Task WriteResponseAsync(
            HttpContext context,
            HttpStatusCode statusCode,
            ErrorResponse response)
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
