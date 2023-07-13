using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace HEAppE.DataStagingAPI
{
    /// <summary>
    /// Exception middleware
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invoke Middleware
        /// </summary>
        /// <param name="context">Context</param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException e)
            {
                ProblemDetails problem = new();
                problem.Title = "Validation Failed";
                problem.Status = StatusCodes.Status400BadRequest;
                problem.Extensions.Add("errors", e.Errors.Select(x => new
                {
                    x.PropertyName,
                    x.ErrorMessage
                }));

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(problem);
            }
            catch (Exception e)
            {
                ProblemDetails problem = new ProblemDetails();
                problem.Title = "Internal Server Error";
                problem.Status = StatusCodes.Status500InternalServerError;
                problem.Extensions.Add("errors", e.Message);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(problem);
            }
        }
    }
}
