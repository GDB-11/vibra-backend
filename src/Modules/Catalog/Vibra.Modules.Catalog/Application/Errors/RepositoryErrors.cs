namespace Vibra.Modules.Catalog.Application.Errors;

/// <summary>Errores de los repositorios; cada operación tiene el suyo.</summary>
internal closed record RepositoryError(Exception? Cause);

/// <summary>El elemento del cursor no existe en la lista pedida.</summary>
internal sealed record UnknownCursor() : RepositoryError((Exception?)null);

internal sealed record ListArtistsFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record FindArtistFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record ListAlbumsFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record ListArtistAlbumsFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record FindAlbumFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record ListTracksFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record ListAlbumTracksFailed(Exception Exception) : RepositoryError(Exception);

internal sealed record FindTrackFailed(Exception Exception) : RepositoryError(Exception);
