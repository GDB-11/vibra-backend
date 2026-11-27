using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Vibra.Shared.Domain;

namespace Vibra.Shared.Api;

/// <summary>
/// Validación estricta de la cadena de consulta: solo los parámetros admitidos, una vez cada uno.
/// Se ejecuta después de la autorización y antes del enlace de modelos, y devuelve todos los errores.
/// </summary>
public abstract class QueryRulesAttribute : Attribute, IResourceFilter
{
    private const int MaxEchoedNameLength = 50;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var errors = Validate(context.HttpContext.Request.Query);

        if (errors.Count > 0)
            context.Result = Problems.Validation(errors).ToActionResult(context.HttpContext);
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    /// <summary>Error del valor de un parámetro, o <c>null</c> si es válido.</summary>
    protected abstract string? Check(string name, string value);

    /// <summary>Indica si el parámetro está admitido. El nombre distingue mayúsculas.</summary>
    protected abstract bool IsAllowed(string name);

    private List<FieldError> Validate(IQueryCollection query)
    {
        var errors = new List<FieldError>();

        foreach (var (name, values) in query)
        {
            var message = Describe(name, values);

            if (message is not null)
            {
                var field = name.Length > MaxEchoedNameLength ? name[..MaxEchoedNameLength] : name;
                errors.Add(new FieldError(field, message));
            }
        }

        return errors;
    }

    private string? Describe(string name, StringValues values)
    {
        if (!IsAllowed(name))
            return "El parámetro no está permitido.";

        return values.Count == 1 ? Check(name, values.ToString()) : "El parámetro no se puede repetir.";
    }
}

/// <summary>Para las acciones que no admiten parámetros de consulta.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NoQueryAttribute : QueryRulesAttribute
{
    protected override bool IsAllowed(string name) => false;

    protected override string? Check(string name, string value) => null;
}

/// <summary>Parámetros <c>limit</c> y <c>cursor</c> de una lista.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PagedQueryAttribute : QueryRulesAttribute
{
    private const string LimitName = "limit";
    private const string CursorName = "cursor";
    private const string InvalidCursorMessage = "El cursor no es válido.";

    private readonly CursorKind _kind;

    public PagedQueryAttribute(CursorKind kind)
    {
        _kind = kind;
    }

    protected override bool IsAllowed(string name) => name is LimitName or CursorName;

    protected override string? Check(string name, string value)
    {
        if (name == CursorName)
            return PageCursor.TryDecode(value, _kind, out _) ? null : InvalidCursorMessage;

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var limit)
               && limit is >= 1 and <= PageRequest.MaxLimit
            ? null
            : $"El límite debe ser un número entre 1 y {PageRequest.MaxLimit}.";
    }
}
