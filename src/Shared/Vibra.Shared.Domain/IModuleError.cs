namespace Vibra.Shared.Domain;

/// <summary>
/// Contrato común de los errores de módulo. Cada módulo define su propia jerarquía sellada.
/// </summary>
public interface IModuleError
{
    /// <summary>Mensaje seguro para mostrar. Nunca contiene SQL ni <c>Exception.Message</c>.</summary>
    string Message { get; }

    /// <summary>Excepción original, si la hubo. Solo se usa para el registro.</summary>
    Exception? Cause { get; }
}
