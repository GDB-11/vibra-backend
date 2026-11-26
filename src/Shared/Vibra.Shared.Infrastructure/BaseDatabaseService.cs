using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using BindSharp;
using BindSharp.Extensions;
using Dapper;
using Npgsql;

namespace Vibra.Shared.Infrastructure;

/// <summary>
/// Base de los repositorios Dapper. Cada llamada a la base de datos se envuelve en
/// <see cref="TryDbAsync{T,TError}"/> o <see cref="TryTransactionAsync{T,TError}"/>
/// con un error propio de la operación.
/// </summary>
public abstract class BaseDatabaseService(NpgsqlDataSource dataSource)
{
    /// <summary>
    /// Ejecuta la operación y convierte sus excepciones en el error indicado.
    /// La cancelación solicitada no es un error de datos: se propaga como excepción.
    /// </summary>
    protected static Task<Result<T, TError>> TryDbAsync<T, TError>(
        Func<Task<T>> operation,
        Func<Exception, TError> errorFactory,
        CancellationToken cancellationToken) =>
        Result.TryAsync(
            operation,
            exception =>
            {
                if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    ExceptionDispatchInfo.Throw(exception);

                return errorFactory(exception);
            });

    /// <summary>
    /// Ejecuta varias sentencias en una transacción. Solo confirma si el resultado es correcto;
    /// ante un resultado fallido o una excepción, la transacción se deshace al liberarse.
    /// Las sentencias deben usar la conexión y la transacción recibidas (ver <see cref="Command"/>).
    /// </summary>
    protected Task<Result<T, TError>> TryTransactionAsync<T, TError>(
        Func<NpgsqlConnection, NpgsqlTransaction, Task<Result<T, TError>>> operation,
        Func<Exception, TError> errorFactory,
        CancellationToken cancellationToken) =>
        TryDbAsync(
                async () =>
                {
                    await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
                    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                    var result = await operation(connection, transaction);

                    if (result.IsSuccess)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Cancelar a mitad de la confirmación dejaría el resultado sin saberse.
                        await transaction.CommitAsync(CancellationToken.None);
                    }

                    return result;
                },
                errorFactory,
                cancellationToken)
            .BindAsync(result => result);

    /// <summary>Devuelve todas las filas de la consulta.</summary>
    protected Task<IReadOnlyList<TRow>> QueryAsync<TRow>(
        string sql, object? parameters, CancellationToken cancellationToken) =>
        WithConnectionAsync<IReadOnlyList<TRow>>(
            async connection => (await connection.QueryAsync<TRow>(
                Command(sql, parameters, cancellationToken))).AsList(),
            cancellationToken);

    /// <summary>Devuelve la única fila, o <c>null</c> si no hay ninguna.</summary>
    protected Task<TRow?> QuerySingleOrDefaultAsync<TRow>(
        string sql, object? parameters, CancellationToken cancellationToken) =>
        WithConnectionAsync(
            connection => connection.QuerySingleOrDefaultAsync<TRow?>(
                Command(sql, parameters, cancellationToken)),
            cancellationToken);

    /// <summary>Devuelve el primer valor de la primera fila.</summary>
    protected Task<TValue?> ExecuteScalarAsync<TValue>(
        string sql, object? parameters, CancellationToken cancellationToken) =>
        WithConnectionAsync(
            connection => connection.ExecuteScalarAsync<TValue?>(
                Command(sql, parameters, cancellationToken)),
            cancellationToken);

    /// <summary>Ejecuta una sentencia y devuelve el número de filas afectadas.</summary>
    protected Task<int> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken) =>
        WithConnectionAsync(
            connection => connection.ExecuteAsync(Command(sql, parameters, cancellationToken)),
            cancellationToken);

    /// <summary>Crea un comando de Dapper que respeta la cancelación.</summary>
    protected static CommandDefinition Command(
        string sql,
        object? parameters,
        CancellationToken cancellationToken,
        NpgsqlTransaction? transaction = null) =>
        new(sql, parameters, transaction, cancellationToken: cancellationToken);

    /// <summary>Indica si es una violación de unicidad (SQLSTATE 23505) y de qué restricción.</summary>
    protected static bool IsUniqueViolation(Exception exception, [NotNullWhen(true)] out string? constraintName) =>
        IsViolation(exception, PostgresErrorCodes.UniqueViolation, out constraintName);

    /// <summary>Indica si es una violación de clave foránea (SQLSTATE 23503) y de qué restricción.</summary>
    protected static bool IsForeignKeyViolation(Exception exception, [NotNullWhen(true)] out string? constraintName) =>
        IsViolation(exception, PostgresErrorCodes.ForeignKeyViolation, out constraintName);

    private static bool IsViolation(
        Exception exception, string sqlState, [NotNullWhen(true)] out string? constraintName)
    {
        constraintName = exception is PostgresException postgres && postgres.SqlState == sqlState
            ? postgres.ConstraintName
            : null;

        return constraintName is not null;
    }

    private async Task<T> WithConnectionAsync<T>(
        Func<NpgsqlConnection, Task<T>> operation, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await operation(connection);
    }
}
