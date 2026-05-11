using System.Net;
using System.Text.Json;
using ComplianceHub.Application.Common.Exceptions;

namespace ComplianceHub.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        context.Response.StatusCode = exception switch
        {
            ValidationException => (int)HttpStatusCode.BadRequest,
            NotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedException => (int)HttpStatusCode.Unauthorized,
            ForbiddenException => (int)HttpStatusCode.Forbidden,
            ConflictException or RecordInactiveException => (int)HttpStatusCode.Conflict,
            _ => (int)HttpStatusCode.InternalServerError
        };

        object response = exception switch
        {
            ValidationException ve => new { success = false, message = ve.Message, errors = (object)ve.Errors },
            RecordInactiveException rie => new { success = false, message = rie.Message, existingId = rie.ExistingId, isInactive = true },
            _ => new { success = false, message = exception is not (NotFoundException or UnauthorizedException or ForbiddenException or ConflictException)
                ? "An unexpected error occurred." : exception.Message }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
