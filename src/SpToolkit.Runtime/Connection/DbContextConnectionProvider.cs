using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace SpToolkit.Runtime.Connection;

/// <summary>
/// Modo C: extracts the DbConnection (and optional active transaction) from an EF Core DbContext.
/// Does not open, close, or dispose the connection -- the DbContext owns its lifetime.
/// </summary>
internal sealed class DbContextConnectionProvider : IConnectionProvider
{
    private readonly DbContext _dbContext;

    public DbContextConnectionProvider(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async ValueTask<ConnectionLease> AcquireAsync(CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

        return new ConnectionLease(connection, transaction, ownsConnection: false);
    }
}
