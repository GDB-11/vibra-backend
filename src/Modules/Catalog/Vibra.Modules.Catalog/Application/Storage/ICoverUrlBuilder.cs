using Vibra.Modules.Catalog.Application.Contracts;

namespace Vibra.Modules.Catalog.Application.Storage;

internal interface ICoverUrlBuilder
{
    /// <summary>URL de la portada en cada ancho; <c>null</c> sin portada o sin CDN configurado.</summary>
    IReadOnlyList<CoverImage>? Build(Guid? coverKey);
}
