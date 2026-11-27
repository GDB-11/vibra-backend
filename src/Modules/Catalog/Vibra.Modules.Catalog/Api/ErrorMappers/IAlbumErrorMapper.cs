using Vibra.Modules.Catalog.Api.Controllers;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Shared.Api;

namespace Vibra.Modules.Catalog.Api.ErrorMappers;

/// <summary>Mapeador de errores de <see cref="AlbumsController"/>.</summary>
public interface IAlbumErrorMapper : IErrorHttpMapper<CatalogModuleError>;
