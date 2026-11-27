namespace Vibra.Modules.Identity.Application.Security;

internal interface ITokenIssuer
{
    TimeSpan RefreshTokenLifetime { get; }

    AccessToken CreateAccessToken(Guid userCode, DateTimeOffset now);

    /// <summary>Un token aleatorio. Solo se guarda su <see cref="HashRefreshToken"/>.</summary>
    string CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

internal sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
