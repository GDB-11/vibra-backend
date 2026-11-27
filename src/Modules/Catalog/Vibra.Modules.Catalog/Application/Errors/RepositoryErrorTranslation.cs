namespace Vibra.Modules.Catalog.Application.Errors;

/// <summary>Único punto que traduce los errores de los repositorios.</summary>
internal static class RepositoryErrorTranslation
{
    public static CatalogModuleError ToModuleError(RepositoryError error) =>
        error is UnknownCursor
            ? new CatalogModuleError.InvalidCursor()
            : new CatalogModuleError.DataAccess(
                error.Cause ?? new InvalidOperationException($"Error inesperado: {error.GetType().Name}."));
}
