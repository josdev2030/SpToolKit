using SpToolkit.Abstractions.Models;

namespace SpToolkit.Abstractions.Contracts;

/// <summary>
/// Core contract for executing SQL Server stored procedures.
/// The Runtime implements this interface; generated wrapper classes consume it.
/// </summary>
public interface IStoredProcedureExecutor
{
    /// <summary>
    /// Executes a stored procedure that has no result set (insert, update, delete).
    /// Reads output parameters and returns them as <typeparamref name="TOutput"/>.
    /// </summary>
    Task<TOutput> ExecuteAsync<TInput, TOutput>(
        string procedureName,
        TInput input,
        CancellationToken cancellationToken = default)
        where TInput : class
        where TOutput : class, new();

    /// <summary>
    /// Executes a stored procedure and materializes its result set as a list.
    /// </summary>
    Task<IReadOnlyList<TResult>> QueryAsync<TInput, TResult>(
        string procedureName,
        TInput input,
        CancellationToken cancellationToken = default)
        where TInput : class
        where TResult : class, new();

    /// <summary>
    /// Executes a stored procedure and returns the first row, or null if the result set is empty.
    /// </summary>
    Task<TResult?> QuerySingleAsync<TInput, TResult>(
        string procedureName,
        TInput input,
        CancellationToken cancellationToken = default)
        where TInput : class
        where TResult : class, new();

    /// <summary>
    /// Executes a stored procedure that returns both a result set and output parameters.
    /// </summary>
    Task<SpResult<IReadOnlyList<TResult>, TOutput>> QueryWithOutputsAsync<TInput, TResult, TOutput>(
        string procedureName,
        TInput input,
        CancellationToken cancellationToken = default)
        where TInput : class
        where TResult : class, new()
        where TOutput : class, new();
}
