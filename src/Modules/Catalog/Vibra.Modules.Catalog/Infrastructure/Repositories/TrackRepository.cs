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

internal sealed class TrackRepository : BaseDatabaseService, ITrackRepository
{
    public TrackRepository(NpgsqlDataSource dataSource)
        : base(dataSource)
    {
    }

    public Task<Result<IReadOnlyList<Track>, RepositoryError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken) =>
        PageAsync(
            TrackRepositorySql.CursorExists,
            TrackRepositorySql.List,
            new { page.After, Take = page.Limit + 1 },
            page.After,
            exception => new ListTracksFailed(exception),
            cancellationToken);

    public Task<Result<IReadOnlyList<Track>, RepositoryError>> ListByAlbumAsync(
        Guid albumCode, PageRequest page, CancellationToken cancellationToken) =>
        PageAsync(
            TrackRepositorySql.AlbumCursorExists,
            TrackRepositorySql.ListByAlbum,
            new { AlbumCode = albumCode, page.After, Take = page.Limit + 1 },
            page.After,
            exception => new ListAlbumTracksFailed(exception),
            cancellationToken);

    public Task<Result<Track?, RepositoryError>> FindAsync(Guid trackCode, CancellationToken cancellationToken) =>
        TryDbAsync(
            async () =>
            {
                var rows = await QueryAsync<TrackRow>(
                    TrackRepositorySql.Find, new { TrackCode = trackCode }, cancellationToken);

                return (await WithCreditsAsync(rows, cancellationToken)).SingleOrDefault();
            },
            exception => (RepositoryError)new FindTrackFailed(exception),
            cancellationToken);

    private Task<Result<IReadOnlyList<Track>, RepositoryError>> PageAsync(
        string cursorSql,
        string listSql,
        object parameters,
        Guid? after,
        Func<Exception, RepositoryError> errorFactory,
        CancellationToken cancellationToken) =>
        TryDbAsync(
                async () =>
                {
                    if (after is not null
                        && !await ExecuteScalarAsync<bool>(cursorSql, parameters, cancellationToken))
                        return null;

                    var rows = await QueryAsync<TrackRow>(listSql, parameters, cancellationToken);

                    return (IReadOnlyList<Track>?)await WithCreditsAsync(rows, cancellationToken);
                },
                errorFactory,
                cancellationToken)
            .EnsureNotNullAsync(new UnknownCursor());

    // Una sola consulta para los créditos de toda la página.
    private async Task<IReadOnlyList<Track>> WithCreditsAsync(
        IReadOnlyList<TrackRow> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return [];

        var credits = (await QueryAsync<TrackCreditRow>(
                TrackRepositorySql.Credits,
                new { TrackCodes = rows.Select(row => row.TrackCode).ToArray() },
                cancellationToken))
            .ToLookup(credit => credit.TrackCode);

        return [.. rows.Select(row => row.ToTrack(credits[row.TrackCode]))];
    }
}
