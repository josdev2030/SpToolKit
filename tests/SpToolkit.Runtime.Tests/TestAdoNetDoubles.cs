#pragma warning disable CS8765 // Nullability of overridden ADO.NET members
using System.Collections;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace SpToolkit.Runtime.Tests;

/// <summary>
/// Minimal ADO.NET doubles so <see cref="Microsoft.Data.SqlClient.SqlParameter"/> can be added and ExecuteNonQueryAsync observed.
/// </summary>
internal sealed class RecordingDbConnection : DbConnection
{
    public RecordingDbCommand? LastCommand { get; private set; }

    public override string ConnectionString { get; set; } = "";

    public override string Database => "";

    public override string DataSource => "";

    public override string ServerVersion => "0.0";

    public override ConnectionState State => ConnectionState.Open;

    public override void ChangeDatabase(string databaseName) { }

    public override void Open() { }

    public override void Close() { }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        throw new NotSupportedException();

    protected override DbCommand CreateDbCommand()
    {
        var cmd = new RecordingDbCommand(this);
        LastCommand = cmd;
        return cmd;
    }
}

internal sealed class RecordingDbCommand : DbCommand
{
    private readonly RecordingDbConnection _connection;
    private readonly ListDbParameterCollection _parameters = new();

    public RecordingDbCommand(RecordingDbConnection connection) => _connection = connection;

    public int ExecuteNonQueryCallCount { get; private set; }

    public override string CommandText { get; set; } = "";

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; }

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => throw new NotSupportedException();
    }

    protected override DbParameterCollection DbParameterCollection => _parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel() { }

    public override int ExecuteNonQuery()
    {
        ExecuteNonQueryCallCount++;
        return 0;
    }

    public override object? ExecuteScalar() => throw new NotSupportedException();

    public override void Prepare() { }

    protected override DbParameter CreateDbParameter() => new Microsoft.Data.SqlClient.SqlParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        throw new NotSupportedException();

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        _parameters.SimulatePostExecuteOutputValues();
        ExecuteNonQueryCallCount++;
        return 0;
    }
}

internal sealed class ListDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _list = [];

    /// <summary>
    /// Sets synthetic values on OUTPUT parameters so <see cref="ParameterBuilder.PopulateOutput{TOutput}"/> succeeds in tests.
    /// </summary>
    public void SimulatePostExecuteOutputValues()
    {
        foreach (var p in _list)
        {
            if (p.Direction is not (ParameterDirection.Output or ParameterDirection.InputOutput))
                continue;

            if (p is SqlParameter sqlP)
            {
                p.Value = sqlP.SqlDbType switch
                {
                    SqlDbType.Int => 0,
                    SqlDbType.BigInt => 0L,
                    SqlDbType.Bit => false,
                    SqlDbType.NVarChar or SqlDbType.VarChar or SqlDbType.NChar or SqlDbType.Char => "",
                    _ => DBNull.Value,
                };
            }
            else
                p.Value = DBNull.Value;
        }
    }

    public override int Add(object? value)
    {
        _list.Add((DbParameter)value!);
        return _list.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var v in values)
            Add(v!);
    }

    public override void Clear() => _list.Clear();

    public override bool Contains(object value) => _list.Contains((DbParameter)value);

    public override bool Contains(string value) => IndexOf(value) >= 0;

    public override void CopyTo(Array array, int index) => ((ICollection)_list).CopyTo(array, index);

    public override IEnumerator GetEnumerator() => _list.GetEnumerator();

    public override int IndexOf(object value) => _list.IndexOf((DbParameter)value);

    public override int IndexOf(string parameterName)
    {
        for (int i = 0; i < _list.Count; i++)
        {
            if (string.Equals(_list[i].ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public override void Insert(int index, object value) => _list.Insert(index, (DbParameter)value);

    public override void Remove(object value) => _list.Remove((DbParameter)value);

    public override void RemoveAt(int index) => _list.RemoveAt(index);

    public override void RemoveAt(string parameterName)
    {
        var i = IndexOf(parameterName);
        if (i >= 0)
            _list.RemoveAt(i);
    }

    public override int Count => _list.Count;

    public override object SyncRoot => ((ICollection)_list).SyncRoot;

    public override bool IsFixedSize => false;

    public override bool IsReadOnly => false;

    public override bool IsSynchronized => false;

    protected override DbParameter GetParameter(int index) => _list[index];

    protected override DbParameter GetParameter(string parameterName) => _list[IndexOf(parameterName)];

    protected override void SetParameter(int index, DbParameter value) => _list[index] = value;

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var i = IndexOf(parameterName);
        if (i >= 0)
            _list[i] = value;
        else
            Add(value);
    }
}

#pragma warning restore CS8765
