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

/// <summary>Álbumes del catálogo.</summary>
[Route("v1/albums")]
[Authorize]
[ETag]
public sealed class AlbumsController : FunctionalController
{
    private readonly IAlbumService _albums;
    private readonly ITrackService _tracks;
    private readonly IAlbumErrorMapper _errorMapper;

    public AlbumsController(
        IAlbumService albums,
        ITrackService tracks,
        IAlbumErrorMapper errorMapper,
        ILogger<FunctionalController> logger)
        : base(logger)
    {
        _albums = albums;
        _tracks = tracks;
        _errorMapper = errorMapper;
    }

    /// <summary>Lista los álbumes por título.</summary>
    [HttpGet(Name = "listAlbums")]
    [PagedQuery(CursorKind.Album)]
    [ProducesResponseType<Page<AlbumResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> List(
        [FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => _albums.ListAsync(PageRequest.Create(limit, cursor, CursorKind.Album), cancellationToken),
            _errorMapper, nameof(List));

    /// <summary>Devuelve un álbum.</summary>
    [HttpGet("{id:guid}", Name = "getAlbum")]
    [NoQuery]
    [ProducesResponseType<AlbumResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => _albums.GetAsync(id, cancellationToken), _errorMapper, nameof(Get));

    /// <summary>Lista las pistas de un álbum por disco y número.</summary>
    [HttpGet("{id:guid}/tracks", Name = "listAlbumTracks")]
    [PagedQuery(CursorKind.Track)]
    [ProducesResponseType<Page<TrackResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> ListTracks(
        Guid id, [FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => _tracks.ListByAlbumAsync(id, PageRequest.Create(limit, cursor, CursorKind.Track), cancellationToken),
            _errorMapper, nameof(ListTracks));
}
