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

internal sealed class AlbumRepository : BaseDatabaseService, IAlbumRepository
{
    public AlbumRepository(NpgsqlDataSource dataSource)
        : base(dataSource)
    {
    }

    public Task<Result<IReadOnlyList<Album>, RepositoryError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken) =>
        PageAsync(
            AlbumRepositorySql.CursorExists,
            AlbumRepositorySql.List,
            new { page.After, Take = page.Limit + 1 },
            page.After,
            exception => new ListAlbumsFailed(exception),
            cancellationToken);

    public Task<Result<IReadOnlyList<Album>, RepositoryError>> ListByArtistAsync(
        Guid artistCode, PageRequest page, CancellationToken cancellationToken) =>
        PageAsync(
            AlbumRepositorySql.ArtistCursorExists,
            AlbumRepositorySql.ListByArtist,
            new { ArtistCode = artistCode, page.After, Take = page.Limit + 1 },
            page.After,
            exception => new ListArtistAlbumsFailed(exception),
            cancellationToken);

    public Task<Result<Album?, RepositoryError>> FindAsync(Guid albumCode, CancellationToken cancellationToken) =>
        TryDbAsync(
                () => QuerySingleOrDefaultAsync<AlbumRow>(
                    AlbumRepositorySql.Find, new { AlbumCode = albumCode }, cancellationToken),
                exception => (RepositoryError)new FindAlbumFailed(exception),
                cancellationToken)
            .MapAsync(row => row?.ToAlbum());

    private Task<Result<IReadOnlyList<Album>, RepositoryError>> PageAsync(
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

                    var rows = await QueryAsync<AlbumRow>(listSql, parameters, cancellationToken);

                    return (IReadOnlyList<Album>?)[.. rows.Select(row => row.ToAlbum())];
                },
                errorFactory,
                cancellationToken)
            .EnsureNotNullAsync(new UnknownCursor());
}
