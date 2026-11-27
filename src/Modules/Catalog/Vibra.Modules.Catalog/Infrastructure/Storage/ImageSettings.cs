namespace Vibra.Modules.Catalog.Infrastructure.Storage;

/// <summary>Sección <c>Images</c> de la configuración: la CDN del bucket de portadas.</summary>
internal sealed class ImageSettings
{
    public const string Section = "Images";

    private static readonly int[] DefaultCoverWidths = [300, 600, 800];

    /// <summary>URL pública de la CDN. Sin ella, la API no devuelve portadas.</summary>
    public string? BaseUrl { get; set; }

    // Sin valor inicial: el enlazador de configuración añadiría los suyos a los predeterminados.
    public int[]? CoverWidths { get; set; }

    public IReadOnlyList<int> Widths => CoverWidths is { Length: > 0 } ? CoverWidths : DefaultCoverWidths;

    public bool HasValidBaseUrl() =>
        string.IsNullOrEmpty(BaseUrl)
        || (Uri.TryCreate(BaseUrl, UriKind.Absolute, out var url)
            && url.Scheme == Uri.UriSchemeHttps
            && url.Query.Length == 0
            && url.Fragment.Length == 0);
}
