using Microsoft.AspNetCore.Identity;
using Vibra.Modules.Identity.Domain;

namespace Vibra.Modules.Identity.Application.Security;

/// <summary>
/// Calcula un hash aunque no haya cuenta o esté bloqueada, para que el tiempo de respuesta
/// no revele qué correos están registrados.
/// </summary>
internal sealed class DummyPassword
{
    private readonly IPasswordHasher<User> _hasher;
    private readonly Lazy<string> _hash;

    public DummyPassword(IPasswordHasher<User> hasher)
    {
        _hasher = hasher;
        _hash = new(() => hasher.HashPassword(new User(), Guid.NewGuid().ToString()));
    }

    public void Verify(string password) => _hasher.VerifyHashedPassword(new User(), _hash.Value, password);
}
