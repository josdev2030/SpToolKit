using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpToolkit.Abstractions.Contracts;
using SpToolkit.Abstractions.Exceptions;
using SpToolkit.Abstractions.Models;
using SpToolkit.Abstractions.Options;
using SpToolkit.Runtime.Connection;
using SpToolkit.Runtime.Mapping;
using SpToolkit.Runtime.TypeConversion;

namespace SpToolkit.Runtime.Execution;

/// <summary>
/// Implements IStoredProcedureExecutor using ADO.NET and SqlClient.
/// Supports three connection modes:
///   A -- connection string in SpToolkitOptions (executor owns the connection)
///   B -- caller-provided DbConnection (caller owns the connection)
///   C -- EF Core DbContext (DbContext owns the connection)
/// </summary>
public sealed class StoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly ParameterBuilder _parameterBuilder;
    private readonly RowMaterializer _materializer;
    private readonly SpToolkitOptions _options;
    private readonly ILogger<StoredProcedureExecutor>? _logger;

    /// <summary>
    /// Modo A: the executor creates and manages its own SqlConnection
    /// using the connection string from <paramref name="options"/>.
    /// </summary>
    public StoredProcedureExecutor(SpToolkitOptions options, ILogger<StoredProcedureExecutor>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new SpToolkitException(
                "SpToolkitOptions.ConnectionString must be set when using the default constructor. " +
                "Alternatively, provide an external DbConnection.");

        var converter = new TypeConversionService();
        var cache = new MetadataCache(options, converter);
        _parameterBuilder = new ParameterBuilder(cache, converter, options);
        _materializer = new RowMaterializer(cache, converter, options);
        _connectionProvider = new ConnectionStringProvider(options.ConnectionString);
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Modo B: the executor reuses a caller-provided DbConnection (and optional transaction).
    /// The caller is responsible for opening the connection and managing its lifetime.
    /// </summary>
    public StoredProcedureExecutor(SpToolkitOptions options, DbConnection connection, DbTransaction? transaction = null, ILogger<StoredProcedureExecutor>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connection);

        var converter = new TypeConversionService();
        var cache = new MetadataCache(options, converter);
        _parameterBuilder = new ParameterBuilder(cache, converter, options);
        _materializer = new RowMaterializer(cache, converter, options);
        _connectionProvider = new ExternalConnectionProvider(connection, transaction);
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Modo C: the executor extracts the connection and active transaction from an EF Core DbContext.
    /// The DbContext owns the connection lifetime.
    /// </summary>
    public StoredProcedureExecutor(SpToolkitOptions options, DbContext dbContext, ILogger<StoredProcedureExecutor>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dbContext);

        var converter = new TypeConversionService();
        var cache = new MetadataCache(options, converter);
        _parameterBuilder = new ParameterBuilder(cache, converter, options);
        _materializer = new RowMaterializer(cache, converter, options);
        _connectionProvider = new DbContextConnectionProvider(dbContext);
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync<TInput>(
        string procedureName,
        TInput input,
        CancellationToken cancellationToken = default)
        where TInput : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);
        ArgumentNullException.ThrowIfNull(input);

        var sw = StartTiming(procedureName);

        await using var lease = await _connectionProvider.AcquireAsync(cancellationToken);

        await using var command = lease.Connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = lease.Transaction;

        var inputParams = _parameterBuilder.BuildInputParameters(input);

        foreach (var p in inputParams)
            command.Parameters.Add(p);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogError(procedureName, sw, ex);
            throw new SpExecutionException(
                $"Failed to execute stored procedure '{procedureName}': {ex.Message}",
                ex,
                procedureName);
        }

        LogCompleted(procedureName, sw, rowCount: null);
    }

    /// <inheritdoc />
    public async Task<TOutput> ExecuteAsync<TInput, TOutput>(
        string procedureName,
        TInput input,
        CancellationToken cancellationToken = default)
        where TInput : class
        where TOutput : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);
        ArgumentNullException.ThrowIfNull(input);

        var sw = StartTiming(procedureName);

        await using var lease = await _connectionProvider.AcquireAsync(cancellationToken);

        await using var command = lease.Connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = lease.Transaction;

        var inputParams = _parameterBuilder.BuildInputParameters(input);
        var outputParams = _parameterBuilder.BuildOutputParameters(typeof(TOutput));

        foreach (var p in inputParams)
            command.Parameters.Add(p);

        foreach (var p in outputParams)
            command.Parameters.Add(p);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogError(procedureName, sw, ex);
            throw new SpExecutionException(
                $"Failed to execute stored procedure '{procedureName}': {ex.Message}",
                ex,
                procedureName);
        }

        var result = new TOutput();
        _parameterBuilder.PopulateOutput(result, outputParams);

        LogCompleted(procedureName, sw, rowCount: null);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TResult>> QueryAsync<TInput, TResult>(
        string procedureName,
        TInput input,
        CancellationToken cancellationToken = default)
        where TInput : class
        where TResult : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);
        ArgumentNullException.ThrowIfNull(input);

        var sw = StartTiming(procedureName);

        await using var lease = await _connectionProvider.AcquireAsync(cancellationToken);
        await using var command = lease.Connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = lease.Transaction;

        var inputParams = _parameterBuilder.BuildInputParameters(input);
        foreach (var p in inputParams)
            command.Parameters.Add(p);

        DbDataReader reader;
        try
        {
            reader = await command.ExecuteReaderAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogError(procedureName, sw, ex);
            throw new SpExecutionException(
                $"Failed to execute stored procedure '{procedureName}': {ex.Message}",
                ex,
                procedureName);
        }

        IReadOnlyList<TResult> rows;
        await using (reader)
        {
            rows = await _materializer.MaterializeListAsync<TResult>(reader, cancellationToken);
        }

        LogCompleted(procedureName, sw, rows.Count);
        return rows;
    }

    /// <inheritdoc />
    public async Task<TResult?> QuerySingleAsync<TInput, TResult>(
        string procedureName,
        TInput input,
        CancellationToken cancellationToken = default)
        where TInput : class
        where TResult : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);
        ArgumentNullException.ThrowIfNull(input);

        var sw = StartTiming(procedureName);

        await using var lease = await _connectionProvider.AcquireAsync(cancellationToken);
        await using var command = lease.Connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = lease.Transaction;

        var inputParams = _parameterBuilder.BuildInputParameters(input);
        foreach (var p in inputParams)
            command.Parameters.Add(p);

        DbDataReader reader;
        try
        {
            reader = await command.ExecuteReaderAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogError(procedureName, sw, ex);
            throw new SpExecutionException(
                $"Failed to execute stored procedure '{procedureName}': {ex.Message}",
                ex,
                procedureName);
        }

        TResult? result;
        await using (reader)
        {
            result = await _materializer.MaterializeSingleAsync<TResult>(reader, cancellationToken);
        }

        LogCompleted(procedureName, sw, rowCount: result is null ? 0 : 1);
        return result;
    }

    /// <inheritdoc />
    public async Task<SpResult<IReadOnlyList<TResult>, TOutput>> QueryWithOutputsAsync<TInput, TResult, TOutput>(
        string procedureName,
        TInput input,
        CancellationToken cancellationToken = default)
        where TInput : class
        where TResult : class, new()
        where TOutput : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);
        ArgumentNullException.ThrowIfNull(input);

        var sw = StartTiming(procedureName);

        await using var lease = await _connectionProvider.AcquireAsync(cancellationToken);
        await using var command = lease.Connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = lease.Transaction;

        var inputParams = _parameterBuilder.BuildInputParameters(input);
        var outputParams = _parameterBuilder.BuildOutputParameters(typeof(TOutput));

        foreach (var p in inputParams)
            command.Parameters.Add(p);
        foreach (var p in outputParams)
            command.Parameters.Add(p);

        DbDataReader reader;
        try
        {
            reader = await command.ExecuteReaderAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogError(procedureName, sw, ex);
            throw new SpExecutionException(
                $"Failed to execute stored procedure '{procedureName}': {ex.Message}",
                ex,
                procedureName);
        }

        IReadOnlyList<TResult> rows;
        await using (reader)
        {
            rows = await _materializer.MaterializeListAsync<TResult>(reader, cancellationToken);
        }

        // Output parameters are only accessible after the reader is closed
        var output = new TOutput();
        _parameterBuilder.PopulateOutput(output, outputParams);

        LogCompleted(procedureName, sw, rows.Count);

        return new SpResult<IReadOnlyList<TResult>, TOutput>
        {
            Data = rows,
            Output = output
        };
    }

    /// <inheritdoc />
    public async Task<SpResult<TResult?, TOutput>> QuerySingleWithOutputsAsync<TInput, TResult, TOutput>(
        string procedureName,
        TInput input,
        CancellationToken cancellationToken = default)
        where TInput : class
        where TResult : class, new()
        where TOutput : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);
        ArgumentNullException.ThrowIfNull(input);

        var sw = StartTiming(procedureName);

        await using var lease = await _connectionProvider.AcquireAsync(cancellationToken);
        await using var command = lease.Connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = lease.Transaction;

        var inputParams = _parameterBuilder.BuildInputParameters(input);
        var outputParams = _parameterBuilder.BuildOutputParameters(typeof(TOutput));

        foreach (var p in inputParams)
            command.Parameters.Add(p);
        foreach (var p in outputParams)
            command.Parameters.Add(p);

        DbDataReader reader;
        try
        {
            reader = await command.ExecuteReaderAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogError(procedureName, sw, ex);
            throw new SpExecutionException(
                $"Failed to execute stored procedure '{procedureName}': {ex.Message}",
                ex,
                procedureName);
        }

        TResult? row;
        await using (reader)
        {
            row = await _materializer.MaterializeSingleAsync<TResult>(reader, cancellationToken);
        }

        // Output parameters are only accessible after the reader is closed
        var output = new TOutput();
        _parameterBuilder.PopulateOutput(output, outputParams);

        LogCompleted(procedureName, sw, rowCount: row is null ? 0 : 1);

        return new SpResult<TResult?, TOutput>
        {
            Data = row,
            Output = output
        };
    }

    // ── Logging helpers ────────────────────────────────────────────────────

    private Stopwatch StartTiming(string procedureName)
    {
        if (_options.EnableLogging)
            _logger?.LogDebug("Executing SP {ProcedureName}", procedureName);

        return Stopwatch.StartNew();
    }

    private void LogCompleted(string procedureName, Stopwatch sw, int? rowCount)
    {
        if (!_options.EnableLogging) return;

        sw.Stop();
        if (rowCount.HasValue)
            _logger?.LogInformation(
                "SP {ProcedureName} completed in {ElapsedMs}ms ({RowCount} rows)",
                procedureName, sw.ElapsedMilliseconds, rowCount.Value);
        else
            _logger?.LogInformation(
                "SP {ProcedureName} completed in {ElapsedMs}ms",
                procedureName, sw.ElapsedMilliseconds);
    }

    private void LogError(string procedureName, Stopwatch sw, Exception ex)
    {
        if (!_options.EnableLogging) return;

        sw.Stop();
        _logger?.LogError(ex,
            "SP {ProcedureName} failed after {ElapsedMs}ms",
            procedureName, sw.ElapsedMilliseconds);
    }
}
