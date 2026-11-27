namespace Vibra.Shared.Domain;

/// <summary>Página pedida por el cliente, ya validada.</summary>
/// <param name="Limit">Número máximo de elementos.</param>
/// <param name="After">Código público del elemento tras el que continúa; <c>null</c> en la primera página.</param>
public sealed record PageRequest(int Limit, Guid? After)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    /// <summary>Crea la petición a partir de los parámetros que ya comprobó el filtro de la consulta.</summary>
    public static PageRequest Create(int? limit, string? cursor, CursorKind kind) =>
        new(limit ?? DefaultLimit, PageCursor.TryDecode(cursor, kind, out var code) ? code : null);
}
