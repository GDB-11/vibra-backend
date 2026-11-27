using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vibra.Shared.Domain;

namespace Vibra.Shared.Api;

/// <summary>
/// Respuesta para lo que ASP.NET Core rechaza antes de los validadores: JSON mal formado, tipos
/// incorrectos, campos desconocidos o repetidos y cuerpo vacío. Nunca usa los mensajes del framework.
/// </summary>
internal static class InvalidRequestResponse
{
    private const string BodyField = "body";

    public static IActionResult Create(ActionContext context) =>
        Problems.Validation(Describe(context)).ToActionResult(context.HttpContext);

    private static IReadOnlyList<FieldError> Describe(ActionContext context)
    {
        var keys = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry => entry.Key)
            .ToList();

        // Las claves que empiezan por "$" son rutas dentro del JSON y dicen qué falló.
        var jsonPaths = keys.Where(key => key.StartsWith('$')).ToList();

        if (jsonPaths.Count > 0)
        {
            return
            [
                .. jsonPaths.Select(path => path == "$"
                    ? new FieldError(BodyField, "El cuerpo no es un JSON válido.")
                    : new FieldError(path[2..], "El campo no está permitido o su valor no es válido.")),
            ];
        }

        var bodyNames = context.ActionDescriptor.Parameters
            .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(parameter => parameter.Name)
            .ToHashSet();

        if (keys.Any(key => key.Length == 0 || bodyNames.Contains(key)))
            return [new FieldError(BodyField, "El cuerpo de la solicitud es obligatorio.")];

        return [.. keys.Select(key => new FieldError(JsonNamingPolicy.CamelCase.ConvertName(key), "El valor no es válido."))];
    }
}
