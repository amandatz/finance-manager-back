using FinanceManager.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FinanceManager.Api.ErrorHandling;

/// <summary>
/// Maps each <see cref="DomainException"/> subtype to its HTTP status code and a <see cref="ProblemDetails"/> body.
/// Returns <c>false</c> for exceptions it doesn't recognize, so they fall through to <see cref="GlobalExceptionHandler"/>.
/// </summary>
public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message),
            ConflictException conflict => (StatusCodes.Status409Conflict, conflict.Message),
            AuthenticationFailedException auth => (StatusCodes.Status401Unauthorized, auth.Message),
            BusinessRuleViolationException or DomainValidationException =>
                (StatusCodes.Status400BadRequest, exception.Message),
            _ => (0, string.Empty),
        };

        if (statusCode == 0)
        {
            // Not a domain exception we know how to map - let the next handler
            // (GlobalExceptionHandler) deal with it as an unexpected 500.
            return false;
        }

        // Expected, client-facing outcomes. Logged at a level proportional to how
        // actionable/suspicious they are for operators, never at Error - that's reserved
        // for exceptions nobody anticipated.
        var logLevel = exception is AuthenticationFailedException or ConflictException
            ? LogLevel.Warning
            : LogLevel.Information;
        logger.Log(
            logLevel,
            "{ExceptionType} handled while processing {Path}: {Message}",
            exception.GetType().Name,
            httpContext.Request.Path,
            exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier },
        };

        if (exception is BusinessRuleViolationException { Errors.Count: > 1 } businessRule)
        {
            problemDetails.Extensions["errors"] = businessRule.Errors;
        }

        if (exception is DomainValidationException { PropertyName: { } propertyName })
        {
            problemDetails.Extensions["propertyName"] = propertyName;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
