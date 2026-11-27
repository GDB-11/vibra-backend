namespace Vibra.Shared.Domain;

/// <summary>Página de una lista con paginación por cursor.</summary>
/// <param name="Items">Elementos de la página.</param>
/// <param name="NextCursor">Cursor de la página siguiente; <c>null</c> si no hay más.</param>
public sealed record Page<T>(IReadOnlyList<T> Items, string? NextCursor);

/// <summary>Crea páginas a partir de las filas leídas.</summary>
public static class Page
{
    /// <summary>
    /// Crea la página a partir de hasta <c>limit + 1</c> elementos: el sobrante solo indica que hay más.
    /// </summary>
    /// <param name="code">Código público de un elemento, para el cursor.</param>
    public static Page<T> From<T>(IReadOnlyList<T> rows, int limit, CursorKind kind, Func<T, Guid> code) =>
        rows.Count > limit
            ? new Page<T>([.. rows.Take(limit)], PageCursor.Encode(kind, code(rows[limit - 1])))
            : new Page<T>(rows, null);
}
