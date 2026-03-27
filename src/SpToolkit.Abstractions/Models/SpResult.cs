namespace SpToolkit.Abstractions.Models;

/// <summary>
/// Container for stored procedures that return both a result set and output parameters.
/// </summary>
/// <typeparam name="TData">Type holding the result set (typically IReadOnlyList&lt;TRow&gt;).</typeparam>
/// <typeparam name="TOutput">Type holding the output parameter values.</typeparam>
public sealed class SpResult<TData, TOutput>
    where TData : class
    where TOutput : class
{
    public required TData Data { get; init; }
    public required TOutput Output { get; init; }
}
