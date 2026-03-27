namespace SpToolkit.Abstractions.Models;

/// <summary>
/// Singleton request type for stored procedures that take no input parameters.
/// Generated wrappers use EmptyRequest.Instance internally.
/// </summary>
public sealed class EmptyRequest
{
    public static readonly EmptyRequest Instance = new();
    private EmptyRequest() { }
}
