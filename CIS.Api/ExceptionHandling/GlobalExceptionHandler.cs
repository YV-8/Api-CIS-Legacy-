using System.Net.Mime;
using CIS.BusinessLogic.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace CIS.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path.Value
        };

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        return true;
    }

    private (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            AuthenticationRequiredException ex => (StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message),
            NotFoundException ex => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            ForbiddenException ex => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            ConflictException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            UnauthorizedAccessException ex => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            ArgumentOutOfRangeException ex => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server Error",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An error occurred while processing your request.")
        };
    }
}
