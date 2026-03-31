using System.Data.Common;

namespace SpToolkit.Runtime.Connection;

/// <summary>
/// Wraps a DbConnection for a single stored procedure execution.
/// Closes and disposes the connection only when the Runtime owns it (Mode A).
/// When the caller supplies an external connection (Mode B), the connection is left untouched.
/// </summary>
internal sealed class ConnectionLease : IAsyncDisposable
{
    public DbConnection Connection { get; }
    public DbTransaction? Transaction { get; }

    private readonly bool _ownsConnection;

    internal ConnectionLease(DbConnection connection, DbTransaction? transaction, bool ownsConnection)
    {
        Connection = connection;
        Transaction = transaction;
        _ownsConnection = ownsConnection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_ownsConnection)
        {
            await Connection.CloseAsync();
            await Connection.DisposeAsync();
        }
    }
}
