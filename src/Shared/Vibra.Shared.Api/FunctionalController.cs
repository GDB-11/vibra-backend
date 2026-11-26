using System.Security.Claims;
using BindSharp;
using BindSharp.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Vibra.Shared.Domain;

namespace Vibra.Shared.Api;

/// <summary>
/// Controlador base. Cada acción es una sola expresión que entrega el <c>Result</c> de un servicio
/// a uno de estos métodos; aquí viven la conversión a HTTP, el registro de fallos y la lectura de claims.
/// </summary>
[ApiController]
public abstract class FunctionalController(ILogger<FunctionalController> logger) : ControllerBase
{
    private const string SubjectClaim = "sub";

    /// <summary>Ejecuta la operación y convierte su resultado en una respuesta HTTP.</summary>
    protected async Task<IActionResult> ExecuteAsync<T, TError>(
        Func<Task<Result<T, TError>>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        Func<T, IActionResult>? successMapper = null)
        where TError : IModuleError
    {
        var result = await operation().TapErrorAsync(error => LogFailure(operationName, error));

        return result.ToHttpResult(errorMapper, HttpContext, successMapper);
    }

    /// <summary>
    /// Como <see cref="ExecuteAsync{T,TError}"/>, pero entrega el código público (<c>UserCode</c>) del usuario autenticado.
    /// Responde 401 si no hay una identidad autenticada con un id válido.
    /// </summary>
    protected async Task<IActionResult> ExecuteAuthenticatedAsync<T, TError>(
        Func<Guid, Task<Result<T, TError>>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        Func<T, IActionResult>? successMapper = null)
        where TError : IModuleError
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(SubjectClaim);

        if (User.Identity?.IsAuthenticated != true || !Guid.TryParse(subject, out var userId))
        {
            logger.LogWarning("La operación {Operation} se rechazó: sin id de usuario válido", operationName);

            return Problems.Unauthorized().ToActionResult(HttpContext);
        }

        return await ExecuteAsync(() => operation(userId), errorMapper, operationName, successMapper);
    }

    // Solo se registran el nombre de la operación, el tipo de error y la excepción; nunca los datos.
    private void LogFailure<TError>(string operationName, TError error)
        where TError : IModuleError =>
        logger.Log(
            error.Cause is null ? LogLevel.Warning : LogLevel.Error,
            error.Cause,
            "La operación {Operation} falló con {ErrorType}",
            operationName,
            error.GetType().Name);
}
