using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vibra.Modules.Catalog.Application.Errors;
using Vibra.Shared.Api;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Api.ErrorMappers;

internal sealed class ArtistErrorMapper : IArtistErrorMapper
{
    public ProblemDetails Map(CatalogModuleError error) =>
        error switch
        {
            CatalogModuleError.ArtistNotFound => Problems.Create(
                StatusCodes.Status404NotFound, "artist-not-found", "Artista no encontrado", error.Message),
            CatalogModuleError.InvalidCursor => Problems.Validation([new FieldError("cursor", error.Message)]),
            CatalogModuleError.DataAccess => Problems.Unexpected(),
            // Este controlador no los produce.
            CatalogModuleError.AlbumNotFound or CatalogModuleError.TrackNotFound =>
                Problems.Unexpected(),
        };
}
