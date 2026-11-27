using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Application.Errors;

/// <summary>
/// Errores del módulo Catalog. La jerarquía es cerrada: cada mapeador enumera todos los casos y
/// uno nuevo sin tratar no compila.
/// </summary>
public closed record CatalogModuleError : IModuleError
{
    private CatalogModuleError(string message, Exception? cause = null)
    {
        Message = message;
        Cause = cause;
    }

    /// <inheritdoc />
    public string Message { get; }

    /// <inheritdoc />
    public Exception? Cause { get; }

    /// <summary>No hay un artista visible con ese código.</summary>
    public sealed record ArtistNotFound() : CatalogModuleError("No se encontró el artista.");

    /// <summary>No hay un álbum visible con ese código.</summary>
    public sealed record AlbumNotFound() : CatalogModuleError("No se encontró el álbum.");

    /// <summary>No hay una pista visible con ese código.</summary>
    public sealed record TrackNotFound() : CatalogModuleError("No se encontró la pista.");

    /// <summary>El cursor está bien formado, pero no corresponde a ningún elemento de la lista.</summary>
    public sealed record InvalidCursor() : CatalogModuleError("El cursor no es válido.");

    /// <summary>Fallo de acceso a datos. La excepción solo se registra.</summary>
    public sealed record DataAccess(Exception Exception)
        : CatalogModuleError("No se pudo completar la operación.", Exception);
}
