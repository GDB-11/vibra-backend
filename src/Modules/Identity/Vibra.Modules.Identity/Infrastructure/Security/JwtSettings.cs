using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Vibra.Modules.Identity.Infrastructure.Security;

/// <summary>Sección <c>Jwt</c> de la configuración. <c>SigningKey</c> es un secreto: nunca se versiona.</summary>
internal sealed class JwtSettings
{
    public const string Section = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    // HMAC-SHA256 necesita al menos 256 bits.
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    public SymmetricSecurityKey CreateKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
