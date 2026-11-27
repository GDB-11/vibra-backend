using BindSharp;
using Vibra.Modules.Identity.Application.Contracts;
using Vibra.Modules.Identity.Application.Errors;

namespace Vibra.Modules.Identity.Application.Services;

/// <summary>Registro, inicio de sesión y renovación (decisión 007).</summary>
public interface IAuthService
{
    /// <summary>Crea la cuenta e inicia sesión.</summary>
    Task<Result<AuthTokens, IdentityModuleError>> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>Inicia sesión con correo y contraseña.</summary>
    Task<Result<AuthTokens, IdentityModuleError>> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken);

    /// <summary>Cambia un token de renovación por tokens nuevos.</summary>
    Task<Result<AuthTokens, IdentityModuleError>> RefreshAsync(
        RefreshRequest request, CancellationToken cancellationToken);
}
