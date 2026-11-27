namespace Vibra.Modules.Catalog.Domain;

/// <summary>Artista, identificado fuera del backend por su código público.</summary>
internal sealed class Artist
{
    public Guid ArtistCode { get; set; }

    public string Name { get; set; } = string.Empty;
}
