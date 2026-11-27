namespace Vibra.Modules.Catalog.Application.Contracts;

/// <summary>Artista.</summary>
public sealed record ArtistResponse(Guid Id, string Name);

/// <summary>Portada en un ancho concreto, en píxeles.</summary>
public sealed record CoverImage(int Width, Uri Url);

/// <summary>Álbum. El precio va en centavos con su código de moneda ISO 4217.</summary>
public sealed record AlbumResponse(
    Guid Id,
    string Title,
    ArtistResponse Artist,
    DateOnly ReleaseDate,
    int TrackCount,
    int PriceCents,
    string Currency,
    string? StoreProductId,
    IReadOnlyList<CoverImage>? Cover);

/// <summary>Álbum al que pertenece una pista.</summary>
public sealed record TrackAlbum(Guid Id, string Title, IReadOnlyList<CoverImage>? Cover);

/// <summary>Pista. El precio va en centavos con su código de moneda ISO 4217.</summary>
public sealed record TrackResponse(
    Guid Id,
    string Title,
    TrackAlbum Album,
    IReadOnlyList<ArtistResponse> Artists,
    int DiscNumber,
    int TrackNumber,
    int DurationMs,
    int PriceCents,
    string Currency,
    string? StoreProductId);
