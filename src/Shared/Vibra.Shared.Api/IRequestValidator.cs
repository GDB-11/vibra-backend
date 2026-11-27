using Vibra.Shared.Domain;

namespace Vibra.Shared.Api;

/// <summary>Base sin tipo que usa <see cref="RequestValidationFilter"/>. Implementa la versión genérica.</summary>
public interface IRequestValidator
{
    /// <summary>Devuelve todos los errores de la solicitud.</summary>
    IReadOnlyList<FieldError> Validate(object request);
}

/// <summary>
/// Valida el cuerpo de una solicitud antes de la acción. Cada tipo de cuerpo necesita uno, registrado
/// como singleton. Los campos pueden llegar como <c>null</c> aunque el tipo no lo admita.
/// </summary>
public interface IRequestValidator<in TRequest> : IRequestValidator
    where TRequest : class
{
    /// <summary>Devuelve todos los errores; una lista vacía si la solicitud es válida.</summary>
    IReadOnlyList<FieldError> Validate(TRequest request);

    IReadOnlyList<FieldError> IRequestValidator.Validate(object request) => Validate((TRequest)request);
}
