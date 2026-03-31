namespace SpToolkit.Abstractions.Models;

/// <summary>
/// Container for stored procedures that return both a result set and output parameters.
/// </summary>
/// <typeparam name="TData">Type holding the result set (e.g. <see cref="System.Collections.Generic.IReadOnlyList{T}"/> or a nullable row type).</typeparam>
/// <typeparam name="TOutput">Type holding the output parameter values.</typeparam>
public sealed class SpResult<TData, TOutput>
    where TOutput : class
{
    /// <summary>The materialized result set rows, or a single nullable row for <c>QuerySingleWithOutputs</c> patterns.</summary>
    public required TData Data { get; init; }

    /// <summary>The output parameter values populated after the stored procedure completes.</summary>
    public required TOutput Output { get; init; }
}
