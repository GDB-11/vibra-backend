using BindSharp;
using Vibra.Modules.Catalog.Application.Contracts;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Application.Services;

/// <summary>Lectura de álbumes.</summary>
public interface IAlbumService
{
    /// <summary>Lista los álbumes.</summary>
    Task<Result<Page<AlbumResponse>, CatalogModuleError>> ListAsync(
        PageRequest page, CancellationToken cancellationToken);

    /// <summary>Lista los álbumes de un artista principal.</summary>
    Task<Result<Page<AlbumResponse>, CatalogModuleError>> ListByArtistAsync(
        Guid artistCode, PageRequest page, CancellationToken cancellationToken);

    /// <summary>Devuelve un álbum.</summary>
    Task<Result<AlbumResponse, CatalogModuleError>> GetAsync(Guid albumCode, CancellationToken cancellationToken);
}
