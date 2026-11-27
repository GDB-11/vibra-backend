using Vibra.Shared.Domain;

namespace Vibra.Modules.Identity.Application.Errors;

/// <summary>
/// Errores del módulo Identity. No se llama <c>IdentityError</c> para no chocar con el tipo
/// de ASP.NET Core Identity. La jerarquía es cerrada: un caso nuevo sin tratar en el mapeador no compila.
/// </summary>
public closed record IdentityModuleError : IModuleError
{
    private IdentityModuleError(string message, Exception? cause = null)
    {
        Message = message;
        Cause = cause;
    }

    /// <inheritdoc />
    public string Message { get; }

    /// <inheritdoc />
    public Exception? Cause { get; }

    /// <summary>Datos que solo Identity puede rechazar, como una contraseña débil.</summary>
    public sealed record Validation(IReadOnlyList<FieldError> Errors)
        : IdentityModuleError("Revisa los campos indicados.");

    /// <summary>Ya hay una cuenta con ese correo.</summary>
    public sealed record DuplicateEmail() : IdentityModuleError("Ya existe una cuenta con ese correo.");

    /// <summary>
    /// Correo desconocido, contraseña errónea o cuenta bloqueada. Es un único error para no
    /// revelar qué cuentas existen.
    /// </summary>
    public sealed record InvalidCredentials() : IdentityModuleError("El correo o la contraseña no son correctos.");

    /// <summary>Token de renovación desconocido, caducado, revocado o reutilizado.</summary>
    public sealed record InvalidRefreshToken() : IdentityModuleError("La sesión no es válida. Inicia sesión de nuevo.");

    /// <summary>Fallo de acceso a datos. La excepción solo se registra.</summary>
    public sealed record DataAccess(Exception Exception)
        : IdentityModuleError("No se pudo completar la operación.", Exception);
}
