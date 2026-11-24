using Microsoft.AspNetCore.Mvc;
using Vibra.Shared.Domain;

namespace Vibra.Shared.Api;

/// <summary>Convierte los errores de un módulo en Problem Details (RFC 7807).</summary>
public interface IErrorHttpMapper<in TError>
    where TError : IModuleError
{
    /// <summary>
    /// Devuelve el problema que corresponde al error, con su código de estado.
    /// El <c>traceId</c> se añade después, en un único punto.
    /// </summary>
    ProblemDetails Map(TError error);
}
