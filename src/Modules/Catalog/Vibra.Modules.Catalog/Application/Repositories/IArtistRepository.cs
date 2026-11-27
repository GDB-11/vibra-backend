using BindSharp;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Modules.Catalog.Domain;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Application.Repositories;

internal interface IArtistRepository
{
    /// <summary>Artistas visibles por nombre. Devuelve hasta <c>Limit + 1</c>.</summary>
    Task<Result<IReadOnlyList<Artist>, RepositoryError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken);

    Task<Result<Artist?, RepositoryError>> FindAsync(Guid artistCode, CancellationToken cancellationToken);
}
