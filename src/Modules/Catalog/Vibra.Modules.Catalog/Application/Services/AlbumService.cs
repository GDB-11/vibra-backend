using BindSharp;
using BindSharp.Extensions;
using Vibra.Modules.Catalog.Application.Contracts;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Modules.Catalog.Application.Repositories;
using Vibra.Modules.Catalog.Application.Storage;
using Vibra.Modules.Catalog.Domain;
using Vibra.Shared.Domain;
using static Vibra.Modules.Catalog.Application.Errors.RepositoryErrorTranslation;

namespace Vibra.Modules.Catalog.Application.Services;

internal sealed class AlbumService : IAlbumService
{
    private readonly IAlbumRepository _albums;
    private readonly IArtistRepository _artists;
    private readonly ICoverUrlBuilder _covers;

    public AlbumService(IAlbumRepository albums, IArtistRepository artists, ICoverUrlBuilder covers)
    {
        _albums = albums;
        _artists = artists;
        _covers = covers;
    }

    public Task<Result<Page<AlbumResponse>, CatalogModuleError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken) =>
        _albums.ListAsync(page, cancellationToken)
            .MapErrorAsync(ToModuleError)
            .MapAsync(albums => ToPage(albums, page));

    public Task<Result<Page<AlbumResponse>, CatalogModuleError>> ListByArtistAsync(
        Guid artistCode, PageRequest page, CancellationToken cancellationToken) =>
        _artists.FindAsync(artistCode, cancellationToken)
            .MapErrorAsync(ToModuleError)
            .EnsureNotNullAsync(new CatalogModuleError.ArtistNotFound())
            .BindAsync(_ => _albums.ListByArtistAsync(artistCode, page, cancellationToken)
                .MapErrorAsync(ToModuleError))
            .MapAsync(albums => ToPage(albums, page));

    public Task<Result<AlbumResponse, CatalogModuleError>> GetAsync(
        Guid albumCode, CancellationToken cancellationToken) =>
        _albums.FindAsync(albumCode, cancellationToken)
            .MapErrorAsync(ToModuleError)
            .EnsureNotNullAsync(new CatalogModuleError.AlbumNotFound())
            .MapAsync(ToResponse);

    private Page<AlbumResponse> ToPage(IReadOnlyList<Album> albums, PageRequest page) =>
        Page.From([.. albums.Select(ToResponse)], page.Limit, CursorKind.Album, album => album.Id);

    private AlbumResponse ToResponse(Album album) =>
        new(
            album.AlbumCode,
            album.Title,
            new ArtistResponse(album.Artist.ArtistCode, album.Artist.Name),
            album.ReleaseDate,
            album.TrackCount,
            album.PriceCents,
            album.Currency,
            album.StoreProductId,
            _covers.Build(album.CoverKey));
}
