using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Vibra.Modules.Identity.Api.ErrorMappers;
using Vibra.Modules.Identity.Application.Contracts;
using Vibra.Modules.Identity.Application.Services;
using Vibra.Shared.Api;

namespace Vibra.Modules.Identity.Api.Controllers;

/// <summary>Cuentas y sesiones.</summary>
[Route("v1/auth")]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController : FunctionalController
{
    private readonly IAuthService _auth;
    private readonly IAuthErrorMapper _errorMapper;

    public AuthController(IAuthService auth, IAuthErrorMapper errorMapper, ILogger<FunctionalController> logger)
        : base(logger)
    {
        _auth = auth;
        _errorMapper = errorMapper;
    }

    /// <summary>Crea una cuenta e inicia sesión.</summary>
    [HttpPost("register", Name = "register")]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => _auth.RegisterAsync(request, cancellationToken), _errorMapper, nameof(Register),
            tokens => StatusCode(StatusCodes.Status201Created, tokens));

    /// <summary>Inicia sesión con correo y contraseña.</summary>
    [HttpPost("login", Name = "login")]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => _auth.LoginAsync(request, cancellationToken), _errorMapper, nameof(Login));

    /// <summary>Cambia un token de renovación por tokens nuevos.</summary>
    [HttpPost("refresh", Name = "refreshSession")]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(() => _auth.RefreshAsync(request, cancellationToken), _errorMapper, nameof(Refresh));
}
