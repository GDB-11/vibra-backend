using Microsoft.Extensions.Options;
using Vibra.Modules.Catalog.Application.Contracts;
using Vibra.Modules.Catalog.Application.Storage;

namespace Vibra.Modules.Catalog.Infrastructure.Storage;

/// <summary>Construye las URL <c>covers/{CoverKey}/{width}.jpg</c> (decisión 003).</summary>
internal sealed class CoverUrlBuilder : ICoverUrlBuilder
{
    private readonly IOptions<ImageSettings> _settings;

    public CoverUrlBuilder(IOptions<ImageSettings> settings)
    {
        _settings = settings;
    }

    public IReadOnlyList<CoverImage>? Build(Guid? coverKey)
    {
        var settings = _settings.Value;

        if (coverKey is not { } key || string.IsNullOrEmpty(settings.BaseUrl))
            return null;

        var root = settings.BaseUrl.TrimEnd('/');

        return [.. settings.Widths.Select(width => new CoverImage(width, new Uri($"{root}/covers/{key}/{width}.jpg")))];
    }
}
