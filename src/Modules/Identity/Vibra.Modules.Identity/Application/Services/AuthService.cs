using System.Runtime.ExceptionServices;
using BindSharp;
using BindSharp.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Vibra.Modules.Identity.Application.Contracts;
using Vibra.Modules.Identity.Application.Errors;
using Vibra.Modules.Identity.Application.Repositories;
using Vibra.Modules.Identity.Application.Security;
using Vibra.Modules.Identity.Domain;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Identity.Application.Services;

internal sealed class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenIssuer _tokens;
    private readonly DummyPassword _dummyPassword;
    private readonly IOptions<IdentityOptions> _options;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        UserManager<User> userManager,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        ITokenIssuer tokens,
        DummyPassword dummyPassword,
        IOptions<IdentityOptions> options,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _users = users;
        _refreshTokens = refreshTokens;
        _tokens = tokens;
        _dummyPassword = dummyPassword;
        _options = options;
        _timeProvider = timeProvider;
    }

    public Task<Result<AuthTokens, IdentityModuleError>> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken) =>
        CreateUserAsync(request, cancellationToken)
            .BindAsync(user => StartSessionAsync(user, cancellationToken));

    public Task<Result<AuthTokens, IdentityModuleError>> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken) =>
        CheckCredentialsAsync(request, cancellationToken)
            .BindAsync(user => StartSessionAsync(user, cancellationToken));

    public Task<Result<AuthTokens, IdentityModuleError>> RefreshAsync(
        RefreshRequest request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var replacement = _tokens.CreateRefreshToken();
        var replacementExpiresAt = now + _tokens.RefreshTokenLifetime;

        return _refreshTokens.RotateAsync(
                _tokens.HashRefreshToken(request.RefreshToken),
                _tokens.HashRefreshToken(replacement),
                replacementExpiresAt,
                now,
                cancellationToken)
            .MapErrorAsync(ToModuleError)
            .BindAsync(outcome => outcome.UserCode is { } userCode
                ? Result<AuthTokens, IdentityModuleError>.Success(
                    Session(userCode, replacement, replacementExpiresAt, now))
                : new IdentityModuleError.InvalidRefreshToken());
    }

    private Task<Result<User, IdentityModuleError>> CreateUserAsync(
        RegisterRequest request, CancellationToken cancellationToken)
    {
        // La entrada ya llega validada; aquí solo se quitan los espacios sobrantes.
        var user = new User { Email = request.Email.Trim(), DisplayName = request.DisplayName.Trim() };

        return TryIdentityAsync(() => _userManager.CreateAsync(user, request.Password), cancellationToken)
            .BindAsync(created => created.Succeeded
                ? Result<User, IdentityModuleError>.Success(user)
                : ToModuleError(created));
    }

    private Task<Result<User, IdentityModuleError>> CheckCredentialsAsync(
        LoginRequest request, CancellationToken cancellationToken) =>
        TryIdentityAsync(
                async () =>
                {
                    var user = await _userManager.FindByEmailAsync(request.Email.Trim());

                    if (user is null || await _userManager.IsLockedOutAsync(user))
                    {
                        _dummyPassword.Verify(request.Password);
                        return null;
                    }

                    var matches = await _userManager.CheckPasswordAsync(user, request.Password);

                    return new PasswordCheck(user, matches);
                },
                cancellationToken)
            .BindAsync(check => check is null
                ? Task.FromResult(InvalidCredentials())
                : RecordAttemptAsync(check, cancellationToken));

    private Task<Result<User, IdentityModuleError>> RecordAttemptAsync(
        PasswordCheck check, CancellationToken cancellationToken)
    {
        if (!check.Matches)
        {
            var lockout = _options.Value.Lockout;

            return _users.RecordFailedAccessAsync(
                    check.User.UserId,
                    lockout.MaxFailedAccessAttempts,
                    _timeProvider.GetUtcNow() + lockout.DefaultLockoutTimeSpan,
                    cancellationToken)
                .MapErrorAsync(ToModuleError)
                .BindAsync(_ => InvalidCredentials());
        }

        return _users.ResetFailedAccessAsync(check.User.UserId, cancellationToken)
            .MapErrorAsync(ToModuleError)
            .MapAsync(_ => check.User);
    }

    // Un inicio de sesión abre una familia nueva de tokens de renovación.
    private Task<Result<AuthTokens, IdentityModuleError>> StartSessionAsync(
        User user, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var refreshToken = _tokens.CreateRefreshToken();
        var expiresAt = now + _tokens.RefreshTokenLifetime;

        return _refreshTokens.CreateAsync(
                new NewRefreshToken(user.UserId, _tokens.HashRefreshToken(refreshToken), Guid.NewGuid(), expiresAt),
                cancellationToken)
            .MapErrorAsync(ToModuleError)
            .MapAsync(_ => Session(user.UserCode, refreshToken, expiresAt, now));
    }

    private AuthTokens Session(
        Guid userCode, string refreshToken, DateTimeOffset refreshExpiresAt, DateTimeOffset now)
    {
        var access = _tokens.CreateAccessToken(userCode, now);

        return new AuthTokens(access.Value, access.ExpiresAt, refreshToken, refreshExpiresAt);
    }

    // UserManager lanza excepciones; aquí vuelven a ser un Result. La cancelación pedida se propaga.
    private static Task<Result<T, IdentityModuleError>> TryIdentityAsync<T>(
        Func<Task<T>> operation, CancellationToken cancellationToken) =>
        Result.TryAsync<T, IdentityModuleError>(
            operation,
            exception =>
            {
                if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    ExceptionDispatchInfo.Throw(exception);

                return new IdentityModuleError.DataAccess(exception);
            });

    private static Result<User, IdentityModuleError> InvalidCredentials() =>
        new IdentityModuleError.InvalidCredentials();

    // Único punto que traduce los errores de los repositorios.
    private static IdentityModuleError ToModuleError(RepositoryError error) =>
        new IdentityModuleError.DataAccess(
            error.Cause ?? new InvalidOperationException($"Error inesperado: {error.GetType().Name}."));

    private static IdentityModuleError ToModuleError(IdentityResult result)
    {
        var codes = result.Errors.Select(error => error.Code).ToList();

        // Con el correo como nombre de usuario, Identity puede detectar el duplicado por cualquiera.
        if (codes.Contains(nameof(IdentityErrorDescriber.DuplicateEmail))
            || codes.Contains(nameof(IdentityErrorDescriber.DuplicateUserName)))
            return new IdentityModuleError.DuplicateEmail();

        return codes.All(code => code.StartsWith("Password", StringComparison.Ordinal))
            ? new IdentityModuleError.Validation(
                [.. result.Errors.Select(error => new FieldError("password", error.Description))])
            : new IdentityModuleError.DataAccess(
                new InvalidOperationException($"Identity rechazó al usuario: {string.Join(", ", codes)}."));
    }

    private sealed record PasswordCheck(User User, bool Matches);
}
