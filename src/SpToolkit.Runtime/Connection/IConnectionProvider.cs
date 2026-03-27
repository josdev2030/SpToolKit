namespace SpToolkit.Runtime.Connection;

/// <summary>
/// Internal abstraction for acquiring a database connection lease.
/// Implementations differ based on how the connection is sourced
/// (connection string, external DbConnection, or EF Core DbContext in Phase 7).
/// </summary>
internal interface IConnectionProvider
{
    ValueTask<ConnectionLease> AcquireAsync(CancellationToken cancellationToken);
}
