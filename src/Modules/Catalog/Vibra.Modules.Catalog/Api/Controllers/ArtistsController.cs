using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Vibra.Modules.Catalog.Api.ErrorMappers;
using Vibra.Modules.Catalog.Application.Contracts;
using Vibra.Modules.Catalog.Application.Services;
using Vibra.Shared.Api;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Api.Controllers;

/// <summary>Artistas del catálogo.</summary>
[Route("v1/artists")]
[Authorize]
[ETag]
public sealed class ArtistsController : FunctionalController
{
    private readonly IArtistService _artists;
    private readonly IAlbumService _albums;
    private readonly IArtistErrorMapper _errorMapper;

    public ArtistsController(
        IArtistService artists,
        IAlbumService albums,
        IArtistErrorMapper errorMapper,
        ILogger<FunctionalController> logger)
        : base(logger)
    {
        _artists = artists;
        _albums = albums;
        _errorMapper = errorMapper;
    }

    /// <summary>Lista los artistas por nombre.</summary>
    [HttpGet(Name = "listArtists")]
    [PagedQuery(CursorKind.Artist)]
    [ProducesResponseType<Page<ArtistResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> List(
        [FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => _artists.ListAsync(PageRequest.Create(limit, cursor, CursorKind.Artist), cancellationToken),
            _errorMapper, nameof(List));

    /// <summary>Devuelve un artista.</summary>
    [HttpGet("{id:guid}", Name = "getArtist")]
    [NoQuery]
    [ProducesResponseType<ArtistResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => _artists.GetAsync(id, cancellationToken), _errorMapper, nameof(Get));

    /// <summary>Lista los álbumes de un artista principal por fecha de salida.</summary>
    [HttpGet("{id:guid}/albums", Name = "listArtistAlbums")]
    [PagedQuery(CursorKind.Album)]
    [ProducesResponseType<Page<AlbumResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> ListAlbums(
        Guid id, [FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => _albums.ListByArtistAsync(id, PageRequest.Create(limit, cursor, CursorKind.Album), cancellationToken),
            _errorMapper, nameof(ListAlbums));
}
