using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vibra.Modules.Identity.Application.Errors;
using Vibra.Shared.Api;

namespace Vibra.Modules.Identity.Api.ErrorMappers;

internal sealed class AuthErrorMapper : IAuthErrorMapper
{
    public ProblemDetails Map(IdentityModuleError error) =>
        error switch
        {
            IdentityModuleError.Validation validation => Problems.Validation(validation.Errors),
            IdentityModuleError.DuplicateEmail => Problems.Create(
                StatusCodes.Status409Conflict, "duplicate-email", "Correo ya registrado", error.Message),
            IdentityModuleError.InvalidCredentials => Problems.Create(
                StatusCodes.Status401Unauthorized, "invalid-credentials", "Credenciales no válidas", error.Message),
            IdentityModuleError.InvalidRefreshToken => Problems.Create(
                StatusCodes.Status401Unauthorized, "invalid-refresh-token", "Sesión no válida", error.Message),
            _ => Problems.Unexpected(),
        };
}
