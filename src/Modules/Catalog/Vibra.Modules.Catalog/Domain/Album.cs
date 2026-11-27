namespace Vibra.Modules.Catalog.Domain;

/// <summary>Álbum visible del catálogo.</summary>
internal sealed class Album
{
    public Guid AlbumCode { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Artista principal.</summary>
    public Artist Artist { get; set; } = new();

    public DateOnly ReleaseDate { get; set; }

    public Guid? CoverKey { get; set; }

    /// <summary>Solo cuenta las pistas <c>Ready</c>.</summary>
    public int TrackCount { get; set; }

    public int PriceCents { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string? StoreProductId { get; set; }
}
