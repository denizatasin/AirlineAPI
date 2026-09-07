using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AirlineAPI.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger=logger;
    }
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "System error! Request path {Path}, Error Message:{Message}",httpContext.Request.Path, exception.Message);
        var problemDetails=new ProblemDetails
        {
            Status=StatusCodes.Status500InternalServerError,
            Title="Internal Server Error",
            Detail="An error occured with the server during transaction. Please contact with your system administrator.",
            Instance=httpContext.Request.Path
        };  
        httpContext.Response.StatusCode=problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;  
    }
}