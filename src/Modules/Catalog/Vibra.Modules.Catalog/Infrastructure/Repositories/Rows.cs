using Vibra.Modules.Catalog.Domain;

namespace Vibra.Modules.Catalog.Infrastructure.Repositories;

internal sealed record ArtistRow(Guid ArtistCode, string Name)
{
    public Artist ToArtist() => new() { ArtistCode = ArtistCode, Name = Name };
}

internal sealed record AlbumRow(
    Guid AlbumCode,
    string Title,
    Guid ArtistCode,
    string ArtistName,
    DateOnly ReleaseDate,
    Guid? CoverKey,
    int TrackCount,
    int PriceCents,
    string Currency,
    string? StoreProductId)
{
    public Album ToAlbum() =>
        new()
        {
            AlbumCode = AlbumCode,
            Title = Title,
            Artist = new Artist { ArtistCode = ArtistCode, Name = ArtistName },
            ReleaseDate = ReleaseDate,
            CoverKey = CoverKey,
            TrackCount = TrackCount,
            PriceCents = PriceCents,
            Currency = Currency,
            StoreProductId = StoreProductId,
        };
}

internal sealed record TrackRow(
    Guid TrackCode,
    string Title,
    Guid AlbumCode,
    string AlbumTitle,
    Guid? CoverKey,
    int DiscNumber,
    int TrackNumber,
    int DurationMs,
    int PriceCents,
    string Currency,
    string? StoreProductId)
{
    public Track ToTrack(IEnumerable<TrackCreditRow> credits) =>
        new()
        {
            TrackCode = TrackCode,
            Title = Title,
            AlbumCode = AlbumCode,
            AlbumTitle = AlbumTitle,
            CoverKey = CoverKey,
            Artists = [.. credits.Select(credit => new Artist { ArtistCode = credit.ArtistCode, Name = credit.Name })],
            DiscNumber = DiscNumber,
            TrackNumber = TrackNumber,
            DurationMs = DurationMs,
            PriceCents = PriceCents,
            Currency = Currency,
            StoreProductId = StoreProductId,
        };
}

internal sealed record TrackCreditRow(Guid TrackCode, Guid ArtistCode, string Name);
