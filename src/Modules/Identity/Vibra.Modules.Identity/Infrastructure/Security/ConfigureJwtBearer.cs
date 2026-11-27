using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Vibra.Modules.Identity.Infrastructure.Security;

/// <summary>Valida los JWT con el mismo <see cref="TimeProvider"/> con el que se emiten.</summary>
internal sealed class ConfigureJwtBearer : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IOptions<JwtSettings> _settings;
    private readonly TimeProvider _timeProvider;

    public ConfigureJwtBearer(IOptions<JwtSettings> settings, TimeProvider timeProvider)
    {
        _settings = settings;
        _timeProvider = timeProvider;
    }

    public void Configure(JwtBearerOptions options) => Configure(JwtBearerDefaults.AuthenticationScheme, options);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;

        var value = _settings.Value;

        // El claim "sub" se conserva con su nombre.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = value.Issuer,
            ValidAudience = value.Audience,
            IssuerSigningKey = value.CreateKey(),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            RequireExpirationTime = true,
            // Sin esto, la caducidad se compararía con el reloj del sistema.
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;

                return expires is { } end && now < end && (notBefore is not { } start || now >= start);
            },
        };
    }
}
