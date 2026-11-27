using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Vibra.Modules.Identity.Application.Security;

namespace Vibra.Modules.Identity.Infrastructure.Security;

internal sealed class TokenIssuer : ITokenIssuer
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    private readonly JsonWebTokenHandler _handler = new();
    private readonly IOptions<JwtSettings> _settings;

    public TokenIssuer(IOptions<JwtSettings> settings)
    {
        _settings = settings;
    }

    public TimeSpan RefreshTokenLifetime { get; } = TimeSpan.FromDays(30);

    public AccessToken CreateAccessToken(Guid userCode, DateTimeOffset now)
    {
        // El JWT guarda segundos enteros: la fecha que se devuelve debe coincidir.
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(now.ToUnixTimeSeconds());
        var expiresAt = issuedAt + AccessTokenLifetime;
        var value = _settings.Value;

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = value.Issuer,
            Audience = value.Audience,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userCode.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            },
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(value.CreateKey(), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }

    public string CreateRefreshToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    // Basta SHA-256 sin sal: el token ya tiene 256 bits aleatorios.
    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
