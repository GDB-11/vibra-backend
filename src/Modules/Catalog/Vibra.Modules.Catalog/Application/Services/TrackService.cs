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

internal sealed class TrackService : ITrackService
{
    private readonly ITrackRepository _tracks;
    private readonly IAlbumRepository _albums;
    private readonly ICoverUrlBuilder _covers;

    public TrackService(ITrackRepository tracks, IAlbumRepository albums, ICoverUrlBuilder covers)
    {
        _tracks = tracks;
        _albums = albums;
        _covers = covers;
    }

    public Task<Result<Page<TrackResponse>, CatalogModuleError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken) =>
        _tracks.ListAsync(page, cancellationToken)
            .MapErrorAsync(ToModuleError)
            .MapAsync(tracks => ToPage(tracks, page));

    public Task<Result<Page<TrackResponse>, CatalogModuleError>> ListByAlbumAsync(
        Guid albumCode, PageRequest page, CancellationToken cancellationToken) =>
        _albums.FindAsync(albumCode, cancellationToken)
            .MapErrorAsync(ToModuleError)
            .EnsureNotNullAsync(new CatalogModuleError.AlbumNotFound())
            .BindAsync(_ => _tracks.ListByAlbumAsync(albumCode, page, cancellationToken)
                .MapErrorAsync(ToModuleError))
            .MapAsync(tracks => ToPage(tracks, page));

    public Task<Result<TrackResponse, CatalogModuleError>> GetAsync(
        Guid trackCode, CancellationToken cancellationToken) =>
        _tracks.FindAsync(trackCode, cancellationToken)
            .MapErrorAsync(ToModuleError)
            .EnsureNotNullAsync(new CatalogModuleError.TrackNotFound())
            .MapAsync(ToResponse);

    private Page<TrackResponse> ToPage(IReadOnlyList<Track> tracks, PageRequest page) =>
        Page.From([.. tracks.Select(ToResponse)], page.Limit, CursorKind.Track, track => track.Id);

    private TrackResponse ToResponse(Track track) =>
        new(
            track.TrackCode,
            track.Title,
            new TrackAlbum(track.AlbumCode, track.AlbumTitle, _covers.Build(track.CoverKey)),
            [.. track.Artists.Select(artist => new ArtistResponse(artist.ArtistCode, artist.Name))],
            track.DiscNumber,
            track.TrackNumber,
            track.DurationMs,
            track.PriceCents,
            track.Currency,
            track.StoreProductId);
}
