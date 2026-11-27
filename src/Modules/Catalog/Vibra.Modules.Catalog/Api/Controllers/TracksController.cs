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

/// <summary>Pistas del catálogo.</summary>
[Route("v1/tracks")]
[Authorize]
[ETag]
public sealed class TracksController : FunctionalController
{
    private readonly ITrackService _tracks;
    private readonly ITrackErrorMapper _errorMapper;

    public TracksController(
        ITrackService tracks,
        ITrackErrorMapper errorMapper,
        ILogger<FunctionalController> logger)
        : base(logger)
    {
        _tracks = tracks;
        _errorMapper = errorMapper;
    }

    /// <summary>Lista las pistas por título.</summary>
    [HttpGet(Name = "listTracks")]
    [PagedQuery(CursorKind.Track)]
    [ProducesResponseType<Page<TrackResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> List(
        [FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => _tracks.ListAsync(PageRequest.Create(limit, cursor, CursorKind.Track), cancellationToken),
            _errorMapper, nameof(List));

    /// <summary>Devuelve una pista.</summary>
    [HttpGet("{id:guid}", Name = "getTrack")]
    [NoQuery]
    [ProducesResponseType<TrackResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(() => _tracks.GetAsync(id, cancellationToken), _errorMapper, nameof(Get));
}
