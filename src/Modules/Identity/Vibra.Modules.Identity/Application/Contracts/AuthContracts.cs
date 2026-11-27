using Vibra.Modules.Identity.Application.Security;

namespace Vibra.Modules.Identity.Application.Contracts;

/// <summary>Datos para crear una cuenta.</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);

/// <summary>Credenciales de inicio de sesión.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Token de renovación vigente. Solo se puede usar una vez.</summary>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Tokens de una sesión. Las fechas van en UTC.</summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
