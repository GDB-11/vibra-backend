namespace Vibra.Modules.Catalog.Domain;

/// <summary>Pista visible del catálogo.</summary>
internal sealed class Track
{
    public Guid TrackCode { get; set; }

    public string Title { get; set; } = string.Empty;

    public Guid AlbumCode { get; set; }

    public string AlbumTitle { get; set; } = string.Empty;

    public Guid? CoverKey { get; set; }

    /// <summary>Créditos, en su orden.</summary>
    public IReadOnlyList<Artist> Artists { get; set; } = [];

    public int DiscNumber { get; set; }

    public int TrackNumber { get; set; }

    public int DurationMs { get; set; }

    public int PriceCents { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string? StoreProductId { get; set; }
}
