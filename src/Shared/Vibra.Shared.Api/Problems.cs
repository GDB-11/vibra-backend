using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Vibra.Shared.Api;

/// <summary>Ayudante único para construir Problem Details en los mapeadores de errores.</summary>
public static class Problems
{
    private const string TypePrefix = "urn:vibra:problem:";

    /// <summary>Crea un problema. <paramref name="detail"/> debe ser seguro para el cliente.</summary>
    public static ProblemDetails Create(int status, string type, string title, string detail) =>
        new()
        {
            Type = TypePrefix + type,
            Title = title,
            Status = status,
            Detail = detail,
        };

    /// <summary>Error genérico 500 para fallos de acceso a datos y errores no previstos.</summary>
    public static ProblemDetails Unexpected() =>
        Create(
            StatusCodes.Status500InternalServerError,
            "unexpected",
            "Error interno",
            "No se pudo completar la solicitud. Inténtalo de nuevo más tarde.");

    /// <summary>Error 401 para solicitudes sin una identidad válida.</summary>
    public static ProblemDetails Unauthorized() =>
        Create(
            StatusCodes.Status401Unauthorized,
            "unauthorized",
            "No autorizado",
            "Se requiere una sesión válida.");
}
