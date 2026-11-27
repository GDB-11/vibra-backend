using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Shared.Api;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Api.ErrorMappers;

internal sealed class AlbumErrorMapper : IAlbumErrorMapper
{
    public ProblemDetails Map(CatalogModuleError error) =>
        error switch
        {
            CatalogModuleError.AlbumNotFound => Problems.Create(
                StatusCodes.Status404NotFound, "album-not-found", "Álbum no encontrado", error.Message),
            CatalogModuleError.InvalidCursor => Problems.Validation([new FieldError("cursor", error.Message)]),
            CatalogModuleError.DataAccess => Problems.Unexpected(),
            // Este controlador no los produce.
            CatalogModuleError.ArtistNotFound or CatalogModuleError.TrackNotFound =>
                Problems.Unexpected(),
        };
}
