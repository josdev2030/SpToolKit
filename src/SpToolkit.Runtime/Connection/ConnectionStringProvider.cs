using Microsoft.Data.SqlClient;
using SpToolkit.Abstractions.Exceptions;

namespace SpToolkit.Runtime.Connection;

/// <summary>
/// Mode A: creates a new SqlConnection for each execution and owns its lifetime.
/// </summary>
internal sealed class ConnectionStringProvider : IConnectionProvider
{
    private readonly string _connectionString;

    public ConnectionStringProvider(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async ValueTask<ConnectionLease> AcquireAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return new ConnectionLease(connection, transaction: null, ownsConnection: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await connection.DisposeAsync();
            throw new SpExecutionException(
                "Failed to open SQL Server connection.",
                ex);
        }
    }
}
