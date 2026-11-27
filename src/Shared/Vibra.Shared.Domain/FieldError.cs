namespace Vibra.Shared.Domain;

/// <summary>Error de un campo de entrada.</summary>
/// <param name="Field">Nombre del campo tal como viaja en el JSON.</param>
/// <param name="Message">Mensaje en español, seguro para mostrar.</param>
public sealed record FieldError(string Field, string Message);
