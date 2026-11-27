using BindSharp;
using Vibra.Modules.Identity.Application.Errors;
using Vibra.Modules.Identity.Domain;

namespace Vibra.Modules.Identity.Application.Repositories;

internal interface IUserRepository
{
    /// <summary>Inserta el usuario y devuelve sus claves generadas.</summary>
    Task<Result<UserKeys, RepositoryError>> CreateAsync(User user, CancellationToken cancellationToken);

    Task<Result<User?, RepositoryError>> FindByIdAsync(long userId, CancellationToken cancellationToken);

    Task<Result<User?, RepositoryError>> FindByNormalizedEmailAsync(
        string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Devuelve <c>false</c> si otra escritura cambió <c>ConcurrencyStamp</c>.</summary>
    Task<Result<bool, RepositoryError>> UpdateAsync(
        User user, string newConcurrencyStamp, CancellationToken cancellationToken);

    Task<Result<Unit, RepositoryError>> DeleteAsync(long userId, CancellationToken cancellationToken);

    /// <summary>
    /// Cuenta un intento fallido en una sola sentencia, para que los intentos simultáneos no se pierdan.
    /// Al llegar a <paramref name="maxAttempts"/>, bloquea hasta <paramref name="lockoutEnd"/> y reinicia la cuenta.
    /// </summary>
    Task<Result<Unit, RepositoryError>> RecordFailedAccessAsync(
        long userId, int maxAttempts, DateTimeOffset lockoutEnd, CancellationToken cancellationToken);

    Task<Result<Unit, RepositoryError>> ResetFailedAccessAsync(long userId, CancellationToken cancellationToken);
}

internal sealed record UserKeys(long UserId, Guid UserCode);
