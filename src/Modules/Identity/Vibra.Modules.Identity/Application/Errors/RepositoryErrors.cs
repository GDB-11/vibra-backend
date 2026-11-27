namespace Vibra.Modules.Identity.Application.Errors;

/// <summary>Errores de los repositorios; cada operación tiene el suyo.</summary>
internal closed record RepositoryError(Exception? Cause);

internal sealed record CreateUserFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record EmailAlreadyUsed() : RepositoryError((Exception?)null);

internal sealed record FindUserFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record UpdateUserFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record DeleteUserFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record RecordFailedAccessFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record ResetFailedAccessFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record CreateRefreshTokenFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record RotateRefreshTokenFailed(Exception Exception) : RepositoryError(Exception);
