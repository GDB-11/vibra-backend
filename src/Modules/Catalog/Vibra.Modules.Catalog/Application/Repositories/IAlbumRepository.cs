using BindSharp;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Modules.Catalog.Domain;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Application.Repositories;

internal interface IAlbumRepository
{
    /// <summary>Álbumes visibles por título. Devuelve hasta <c>Limit + 1</c>.</summary>
    Task<Result<IReadOnlyList<Album>, RepositoryError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken);

    /// <summary>Álbumes de un artista principal por fecha de salida. Devuelve hasta <c>Limit + 1</c>.</summary>
    Task<Result<IReadOnlyList<Album>, RepositoryError>> ListByArtistAsync(
        Guid artistCode, PageRequest page, CancellationToken cancellationToken);

    Task<Result<Album?, RepositoryError>> FindAsync(Guid albumCode, CancellationToken cancellationToken);
}
