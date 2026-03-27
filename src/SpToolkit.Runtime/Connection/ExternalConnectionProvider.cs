using System.Data.Common;

namespace SpToolkit.Runtime.Connection;

/// <summary>
/// Modo B: wraps a caller-owned DbConnection (and optional transaction).
/// Does not open, close, or dispose the connection.
/// The caller is responsible for ensuring the connection is open before executing.
/// </summary>
internal sealed class ExternalConnectionProvider : IConnectionProvider
{
    private readonly DbConnection _connection;
    private readonly DbTransaction? _transaction;

    public ExternalConnectionProvider(DbConnection connection, DbTransaction? transaction = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction;
    }

    public ValueTask<ConnectionLease> AcquireAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(new ConnectionLease(_connection, _transaction, ownsConnection: false));
}
