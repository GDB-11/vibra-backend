using BindSharp;
using BindSharp.Extensions;
using Npgsql;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Modules.Catalog.Application.Repositories;
using Vibra.Modules.Catalog.Domain;
using Vibra.Modules.Catalog.Infrastructure.Sql;
using Vibra.Shared.Domain;
using Vibra.Shared.Infrastructure;

namespace Vibra.Modules.Catalog.Infrastructure.Repositories;

internal sealed class ArtistRepository : BaseDatabaseService, IArtistRepository
{
    public ArtistRepository(NpgsqlDataSource dataSource)
        : base(dataSource)
    {
    }

    public Task<Result<IReadOnlyList<Artist>, RepositoryError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken) =>
        TryDbAsync(
                async () =>
                {
                    if (page.After is not null
                        && !await ExecuteScalarAsync<bool>(
                            ArtistRepositorySql.CursorExists, new { page.After }, cancellationToken))
                        return null;

                    var rows = await QueryAsync<ArtistRow>(
                        ArtistRepositorySql.List, new { page.After, Take = page.Limit + 1 }, cancellationToken);

                    return (IReadOnlyList<Artist>?)[.. rows.Select(row => row.ToArtist())];
                },
                exception => (RepositoryError)new ListArtistsFailed(exception),
                cancellationToken)
            .EnsureNotNullAsync(new UnknownCursor());

    public Task<Result<Artist?, RepositoryError>> FindAsync(Guid artistCode, CancellationToken cancellationToken) =>
        TryDbAsync(
                () => QuerySingleOrDefaultAsync<ArtistRow>(
                    ArtistRepositorySql.Find, new { ArtistCode = artistCode }, cancellationToken),
                exception => (RepositoryError)new FindArtistFailed(exception),
                cancellationToken)
            .MapAsync(row => row?.ToArtist());
}
