using BindSharp;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Modules.Catalog.Domain;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Application.Repositories;

internal interface ITrackRepository
{
    /// <summary>Pistas visibles por título. Devuelve hasta <c>Limit + 1</c>.</summary>
    Task<Result<IReadOnlyList<Track>, RepositoryError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken);

    /// <summary>Pistas de un álbum por disco y número. Devuelve hasta <c>Limit + 1</c>.</summary>
    Task<Result<IReadOnlyList<Track>, RepositoryError>> ListByAlbumAsync(
        Guid albumCode, PageRequest page, CancellationToken cancellationToken);

    Task<Result<Track?, RepositoryError>> FindAsync(Guid trackCode, CancellationToken cancellationToken);
}
