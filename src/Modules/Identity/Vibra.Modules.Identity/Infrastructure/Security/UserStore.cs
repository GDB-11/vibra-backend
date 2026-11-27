using System.Globalization;
using BindSharp;
using Microsoft.AspNetCore.Identity;
using Vibra.Modules.Identity.Application.Errors;
using Vibra.Modules.Identity.Application.Repositories;
using Vibra.Modules.Identity.Domain;

namespace Vibra.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Adaptador de ASP.NET Core Identity sobre Dapper (decisiones 007 y 018). No hay columna de
/// nombre de usuario (es el correo), ni de bloqueo habilitado (siempre lo está).
/// </summary>
/// <remarks>
/// Las interfaces de Identity no admiten un <c>Result</c>: un fallo de datos sale como
/// <see cref="UserStoreException"/> y el servicio lo vuelve a convertir en error.
/// </remarks>
internal sealed class UserStore :
    IUserPasswordStore<User>,
    IUserEmailStore<User>,
    IUserSecurityStampStore<User>,
    IUserLockoutStore<User>
{
    private readonly IUserRepository _users;

    public UserStore(IUserRepository users)
    {
        _users = users;
    }

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        var result = await _users.CreateAsync(user, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error is EmailAlreadyUsed
                ? IdentityResult.Failed(new IdentityErrorDescriber().DuplicateEmail(user.Email))
                : throw new UserStoreException(result.Error);
        }

        user.UserId = result.Value.UserId;
        user.UserCode = result.Value.UserCode;

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        var newStamp = Guid.NewGuid().ToString();

        if (!Unwrap(await _users.UpdateAsync(user, newStamp, cancellationToken)))
            return IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure());

        user.ConcurrencyStamp = newStamp;

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        Unwrap(await _users.DeleteAsync(user.UserId, cancellationToken));

        return IdentityResult.Success;
    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        long.TryParse(userId, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? Unwrap(await _users.FindByIdAsync(id, cancellationToken))
            : null;

    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Unwrap(await _users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken));

    public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        FindByEmailAsync(normalizedUserName, cancellationToken);

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserId.ToString(CultureInfo.InvariantCulture));

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Email);

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken) =>
        SetEmailAsync(user, userName, cancellationToken);

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.NormalizedEmail);

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken) =>
        SetNormalizedEmailAsync(user, normalizedName, cancellationToken);

    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Email);

    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken) =>
        Set(() => user.Email = email ?? string.Empty);

    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken) =>
        Set(() => user.NormalizedEmail = normalizedEmail ?? string.Empty);

    // No se confirman correos en la demo.
    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.PasswordHash);

    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken) =>
        Set(() => user.PasswordHash = passwordHash ?? string.Empty);

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash.Length > 0);

    public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.SecurityStamp);

    public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken) =>
        Set(() => user.SecurityStamp = stamp);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken) =>
        Set(() => user.LockoutEnd = lockoutEnd);

    // El inicio de sesión no usa estos contadores en memoria: ver RecordFailedAccessAsync.
    public Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(++user.AccessFailedCount);

    public Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken) =>
        Set(() => user.AccessFailedCount = 0);

    public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public void Dispose()
    {
    }

    private static Task Set(Action change)
    {
        change();

        return Task.CompletedTask;
    }

    private static T Unwrap<T>(Result<T, RepositoryError> result) =>
        result.IsSuccess ? result.Value : throw new UserStoreException(result.Error);
}

internal sealed class UserStoreException : Exception
{
    public UserStoreException(RepositoryError error)
        : base($"Falló el almacén de usuarios ({error.GetType().Name}).", error.Cause)
    {
    }
}
