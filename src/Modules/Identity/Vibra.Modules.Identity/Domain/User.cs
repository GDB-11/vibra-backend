namespace Vibra.Modules.Identity.Domain;

/// <summary>Usuario, tal como lo maneja ASP.NET Core Identity. El nombre de usuario es el correo.</summary>
internal sealed class User
{
    public long UserId { get; set; }

    /// <summary>Código público: es el <c>sub</c> del JWT. <see cref="UserId"/> nunca sale del backend.</summary>
    public Guid UserCode { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string SecurityStamp { get; set; } = string.Empty;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public DateTimeOffset? LockoutEnd { get; set; }

    public int AccessFailedCount { get; set; }
}
