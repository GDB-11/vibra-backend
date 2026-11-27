using BindSharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Vibra.Shared.Api;
using Vibra.Shared.Domain;

namespace Vibra.Shared.Tests.Http;

public closed record ProbeError(string Message, Exception? Cause = null) : IModuleError;

public sealed record ProbeNotFoundError() : ProbeError("El recurso no existe.");

public sealed record ProbeDataError(Exception Exception) : ProbeError("Fallo interno de datos.", Exception);

public sealed class ProbeErrorMapper : IErrorHttpMapper<ProbeError>
{
    public ProblemDetails Map(ProbeError error) =>
        error switch
        {
            ProbeNotFoundError => Problems.Create(404, "probe-not-found", "No encontrado", error.Message),
            ProbeDataError => Problems.Unexpected(),
        };
}

public sealed record ProbeBody(Guid Id);

public sealed class ProbeBodyValidator : IRequestValidator<ProbeBody>
{
    public IReadOnlyList<FieldError> Validate(ProbeBody request) => [];
}

/// <summary>Controlador solo de pruebas para ejercitar la base a través del host real.</summary>
[Route("v1/test-probe")]
public sealed class ProbeController : FunctionalController
{
    public const string Secret = "SECRET-SELECT * FROM users";

    private static readonly ProbeErrorMapper Mapper = new();

    public ProbeController(ILogger<FunctionalController> logger)
        : base(logger)
    {
    }

    [HttpGet("ok")]
    public Task<IActionResult> Succeed() =>
        ExecuteAsync(() => Ok<string>("fine"), Mapper, nameof(Succeed));

    [HttpGet("items/{id:int}", Name = "getProbeItem")]
    public Task<IActionResult> GetItem(int id) =>
        ExecuteAsync(() => Ok<int>(id), Mapper, nameof(GetItem));

    [HttpPost("items")]
    public Task<IActionResult> CreateItem([FromBody] ProbeBody body) =>
        ExecuteAsync(() => Ok<ProbeBody>(body), Mapper, nameof(CreateItem),
            created => CreatedAtRoute("getProbeItem", new { id = 7 }, created));

    [HttpGet("not-found")]
    public Task<IActionResult> Missing() =>
        ExecuteAsync(() => Fail(new ProbeNotFoundError()), Mapper, nameof(Missing));

    [HttpGet("data-error")]
    public Task<IActionResult> DataError() =>
        ExecuteAsync(() => Fail(new ProbeDataError(new InvalidOperationException(Secret))), Mapper, nameof(DataError));

    [HttpGet("throws")]
    public Task<IActionResult> Throws() =>
        ExecuteAsync<string, ProbeError>(() => throw new InvalidOperationException(Secret), Mapper, nameof(Throws));

    [HttpGet("me")]
    public Task<IActionResult> Me() =>
        ExecuteAuthenticatedAsync(userId => Ok<Guid>(userId), Mapper, nameof(Me));

    private static Task<Result<T, ProbeError>> Ok<T>(T value) =>
        Task.FromResult(Result<T, ProbeError>.Success(value));

    private static Task<Result<string, ProbeError>> Fail(ProbeError error) =>
        Task.FromResult(Result<string, ProbeError>.Failure(error));
}
