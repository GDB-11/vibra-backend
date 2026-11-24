using System.Diagnostics;
using BindSharp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vibra.Shared.Domain;

namespace Vibra.Shared.Api;

/// <summary>Conversión de <c>Result</c> a HTTP. Es el único lugar donde se hace <c>Match</c>.</summary>
public static class ResultHttpExtensions
{
    private const string ProblemContentType = "application/problem+json";

    /// <summary>
    /// Convierte el resultado en una respuesta. Sin <paramref name="successMapper"/> responde 200 con el valor.
    /// </summary>
    public static IActionResult ToHttpResult<T, TError>(
        this Result<T, TError> result,
        IErrorHttpMapper<TError> errorMapper,
        HttpContext httpContext,
        Func<T, IActionResult>? successMapper = null)
        where TError : IModuleError =>
        result.Match(
            value => successMapper?.Invoke(value) ?? new OkObjectResult(value),
            error => errorMapper.Map(error).ToActionResult(httpContext));

    /// <summary>Añade el <c>traceId</c> y crea la respuesta <c>application/problem+json</c>.</summary>
    public static IActionResult ToActionResult(this ProblemDetails problem, HttpContext httpContext)
    {
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError,
            ContentTypes = { ProblemContentType },
        };
    }
}
