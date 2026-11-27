using BindSharp;
using BindSharp.Extensions;
using Vibra.Modules.Catalog.Application.Contracts;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Modules.Catalog.Application.Repositories;
using Vibra.Modules.Catalog.Domain;
using Vibra.Shared.Domain;
using static Vibra.Modules.Catalog.Application.Errors.RepositoryErrorTranslation;

namespace Vibra.Modules.Catalog.Application.Services;

internal sealed class ArtistService : IArtistService
{
    private readonly IArtistRepository _artists;

    public ArtistService(IArtistRepository artists)
    {
        _artists = artists;
    }

    public Task<Result<Page<ArtistResponse>, CatalogModuleError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken) =>
        _artists.ListAsync(page, cancellationToken)
            .MapErrorAsync(ToModuleError)
            .MapAsync(artists => ToPage(artists, page));

    public Task<Result<ArtistResponse, CatalogModuleError>> GetAsync(
        Guid artistCode, CancellationToken cancellationToken) =>
        _artists.FindAsync(artistCode, cancellationToken)
            .MapErrorAsync(ToModuleError)
            .EnsureNotNullAsync(new CatalogModuleError.ArtistNotFound())
            .MapAsync(ToResponse);

    private static Page<ArtistResponse> ToPage(IReadOnlyList<Artist> artists, PageRequest page) =>
        Page.From([.. artists.Select(ToResponse)], page.Limit, CursorKind.Artist, artist => artist.Id);

    private static ArtistResponse ToResponse(Artist artist) => new(artist.ArtistCode, artist.Name);
}
