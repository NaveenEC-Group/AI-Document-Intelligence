using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AI_Document_Intelligence.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionFilter(
        ILogger<ApiExceptionFilter> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled)
        {
            return;
        }

        _logger.LogError(context.Exception, "Unhandled API exception.");

        ObjectResult result = context.Exception switch
        {
            InvalidOperationException invalidOperation => new BadRequestObjectResult(
                new { message = invalidOperation.Message }),
            ArgumentException argumentException => new BadRequestObjectResult(
                new { message = argumentException.Message }),
            OperationCanceledException => new ObjectResult(
                new { message = "The request was cancelled." })
            {
                StatusCode = 499
            },
            _ => new ObjectResult(
                new
                {
                    message = _environment.IsDevelopment()
                        ? context.Exception.Message
                        : "An unexpected error occurred."
                })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };

        context.Result = result;
        context.ExceptionHandled = true;
    }
}
