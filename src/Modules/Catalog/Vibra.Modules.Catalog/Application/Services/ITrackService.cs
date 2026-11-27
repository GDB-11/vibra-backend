using BindSharp;
using Vibra.Modules.Catalog.Application.Contracts;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Application.Services;

/// <summary>Lectura de pistas.</summary>
public interface ITrackService
{
    /// <summary>Lista las pistas.</summary>
    Task<Result<Page<TrackResponse>, CatalogModuleError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken);

    /// <summary>Lista las pistas de un álbum.</summary>
    Task<Result<Page<TrackResponse>, CatalogModuleError>> ListByAlbumAsync(
        Guid albumCode, PageRequest page, CancellationToken cancellationToken);

    /// <summary>Devuelve una pista.</summary>
    Task<Result<TrackResponse, CatalogModuleError>> GetAsync(Guid trackCode, CancellationToken cancellationToken);
}
