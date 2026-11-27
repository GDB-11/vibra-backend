using BindSharp;
using Vibra.Modules.Catalog.Application.Contracts;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Application.Services;

/// <summary>Lectura de artistas.</summary>
public interface IArtistService
{
    /// <summary>Lista los artistas.</summary>
    Task<Result<Page<ArtistResponse>, CatalogModuleError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken);

    /// <summary>Devuelve un artista.</summary>
    Task<Result<ArtistResponse, CatalogModuleError>> GetAsync(Guid artistCode, CancellationToken cancellationToken);
}
