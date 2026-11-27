using BindSharp;
using Vibra.Modules.Identity.Application.Errors;

namespace Vibra.Modules.Identity.Application.Repositories;

internal interface IRefreshTokenRepository
{
    Task<Result<Unit, RepositoryError>> CreateAsync(NewRefreshToken token, CancellationToken cancellationToken);

    /// <summary>
    /// Cambia el token por <paramref name="replacementHash"/> en una transacción. Si el token ya se
    /// había usado o revocado, revoca toda su familia.
    /// </summary>
    Task<Result<RotationOutcome, RepositoryError>> RotateAsync(
        string tokenHash,
        string replacementHash,
        DateTimeOffset replacementExpiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

internal sealed record NewRefreshToken(long UserId, string TokenHash, Guid TokenFamily, DateTimeOffset ExpiresAt);

/// <summary>
/// Resultado de una rotación. Un rechazo no es un fallo del repositorio: la transacción debe
/// confirmarse para que la revocación de la familia se guarde.
/// </summary>
internal sealed record RotationOutcome(Guid? UserCode)
{
    public static RotationOutcome Rejected { get; } = new((Guid?)null);
}
