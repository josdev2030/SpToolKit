namespace SpToolkit.Abstractions.Models;

/// <summary>
/// Container for stored procedures that return both a result set and output parameters.
/// </summary>
/// <typeparam name="TData">Type holding the result set (e.g. IReadOnlyList&lt;TRow&gt; or a nullable TRow?).</typeparam>
/// <typeparam name="TOutput">Type holding the output parameter values.</typeparam>
public sealed class SpResult<TData, TOutput>
    where TOutput : class
{
    public required TData Data { get; init; }
    public required TOutput Output { get; init; }
}
