using BindSharp;
using BindSharp.Extensions;
using Npgsql;
using Vibra.Modules.Identity.Application.Errors;
using Vibra.Modules.Identity.Application.Repositories;
using Vibra.Modules.Identity.Domain;
using Vibra.Modules.Identity.Infrastructure.Sql;
using Vibra.Shared.Infrastructure;

namespace Vibra.Modules.Identity.Infrastructure.Repositories;

internal sealed class UserRepository : BaseDatabaseService, IUserRepository
{
    private const string EmailConstraint = "ux_user_normalized_email";

    public UserRepository(NpgsqlDataSource dataSource)
        : base(dataSource)
    {
    }

    public Task<Result<UserKeys, RepositoryError>> CreateAsync(User user, CancellationToken cancellationToken) =>
        TryDbAsync(
            async () =>
            {
                var row = await QuerySingleOrDefaultAsync<UserKeysRow>(UserRepositorySql.Insert, user, cancellationToken);

                return new UserKeys(row!.UserId, row.UserCode);
            },
            exception => IsUniqueViolation(exception, out var constraint) && constraint == EmailConstraint
                ? new EmailAlreadyUsed()
                : (RepositoryError)new CreateUserFailed(exception),
            cancellationToken);

    public Task<Result<User?, RepositoryError>> FindByIdAsync(long userId, CancellationToken cancellationToken) =>
        FindAsync(UserRepositorySql.FindById, new { UserId = userId }, cancellationToken);

    public Task<Result<User?, RepositoryError>> FindByNormalizedEmailAsync(
        string normalizedEmail, CancellationToken cancellationToken) =>
        FindAsync(
            UserRepositorySql.FindByNormalizedEmail, new { NormalizedEmail = normalizedEmail }, cancellationToken);

    public Task<Result<bool, RepositoryError>> UpdateAsync(
        User user, string newConcurrencyStamp, CancellationToken cancellationToken) =>
        TryDbAsync(
            async () => await ExecuteAsync(
                UserRepositorySql.Update,
                new
                {
                    user.UserId,
                    user.Email,
                    user.NormalizedEmail,
                    user.PasswordHash,
                    user.DisplayName,
                    user.SecurityStamp,
                    user.ConcurrencyStamp,
                    NewConcurrencyStamp = newConcurrencyStamp,
                    user.LockoutEnd,
                    user.AccessFailedCount,
                },
                cancellationToken) == 1,
            exception => (RepositoryError)new UpdateUserFailed(exception),
            cancellationToken);

    public Task<Result<Unit, RepositoryError>> DeleteAsync(long userId, CancellationToken cancellationToken) =>
        TryDbAsync(
            async () =>
            {
                await ExecuteAsync(UserRepositorySql.Delete, new { UserId = userId }, cancellationToken);

                return Unit.Value;
            },
            exception => (RepositoryError)new DeleteUserFailed(exception),
            cancellationToken);

    public Task<Result<Unit, RepositoryError>> RecordFailedAccessAsync(
        long userId, int maxAttempts, DateTimeOffset lockoutEnd, CancellationToken cancellationToken) =>
        TryDbAsync(
            async () =>
            {
                await ExecuteAsync(
                    UserRepositorySql.RecordFailedAccess,
                    new
                    {
                        UserId = userId,
                        MaxAttempts = maxAttempts,
                        LockoutEnd = lockoutEnd,
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                    },
                    cancellationToken);

                return Unit.Value;
            },
            exception => (RepositoryError)new RecordFailedAccessFailed(exception),
            cancellationToken);

    public Task<Result<Unit, RepositoryError>> ResetFailedAccessAsync(
        long userId, CancellationToken cancellationToken) =>
        TryDbAsync(
            async () =>
            {
                await ExecuteAsync(
                    UserRepositorySql.ResetFailedAccess,
                    new { UserId = userId, ConcurrencyStamp = Guid.NewGuid().ToString() },
                    cancellationToken);

                return Unit.Value;
            },
            exception => (RepositoryError)new ResetFailedAccessFailed(exception),
            cancellationToken);

    private Task<Result<User?, RepositoryError>> FindAsync(
        string sql, object parameters, CancellationToken cancellationToken) =>
        TryDbAsync(
                () => QuerySingleOrDefaultAsync<UserRow>(sql, parameters, cancellationToken),
                exception => (RepositoryError)new FindUserFailed(exception),
                cancellationToken)
            .MapAsync(row => row?.ToUser());
}
