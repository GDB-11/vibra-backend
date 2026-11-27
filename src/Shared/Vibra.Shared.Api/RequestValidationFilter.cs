using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Vibra.Shared.Api;

/// <summary>
/// Valida el cuerpo antes de ejecutar la acción y responde 400 con todos los errores.
/// Un cuerpo sin validador es un error de programación: se rechaza en lugar de dejarlo pasar.
/// </summary>
internal sealed class RequestValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (parameter.BindingInfo?.BindingSource != BindingSource.Body
                || !context.ActionArguments.TryGetValue(parameter.Name, out var body)
                || body is null)
                continue;

            var validatorType = typeof(IRequestValidator<>).MakeGenericType(parameter.ParameterType);

            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IRequestValidator
                ?? throw new InvalidOperationException($"Falta el validador de {parameter.ParameterType.Name}.");

            var errors = validator.Validate(body);

            if (errors.Count > 0)
            {
                context.Result = Problems.Validation(errors).ToActionResult(context.HttpContext);
                return;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
