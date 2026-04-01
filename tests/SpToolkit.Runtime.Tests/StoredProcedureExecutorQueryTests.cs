using System.Data;
using SpToolkit.Abstractions.Attributes;
using SpToolkit.Abstractions.Models;
using SpToolkit.Abstractions.Options;
using SpToolkit.Runtime.Execution;
using Xunit;

namespace SpToolkit.Runtime.Tests;

public sealed class StoredProcedureExecutorQueryTests
{
    private sealed class UserRow
    {
        [SpResultColumn("Id")]
        public int Id { get; set; }

        [SpResultColumn("Name")]
        public string Name { get; set; } = "";
    }

    private sealed class GetUserRequest
    {
        [SpInput("@Id", SqlDbType.Int)]
        public int Id { get; set; }
    }

    private sealed class UserCountOutput
    {
        [SpOutput("@Total", SqlDbType.Int)]
        public int Total { get; set; }
    }

    private static RecordingDbConnection ConnectionWithRows(string[] columns, object?[][] rows)
    {
        var connection = new RecordingDbConnection();
        connection.NextReader = new FakeDbDataReader(columns, rows);
        return connection;
    }

    // ── QueryAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_empty_result_returns_empty_list()
    {
        var connection = ConnectionWithRows(["Id", "Name"], []);
        var executor = new StoredProcedureExecutor(new SpToolkitOptions(), connection);

        var result = await executor.QueryAsync<EmptyRequest, UserRow>("dbo.GetUsers", EmptyRequest.Instance);

        Assert.Empty(result);
    }

    [Fact]
    public async Task QueryAsync_multiple_rows_returned()
    {
        var connection = ConnectionWithRows(
            ["Id", "Name"],
            [[1, "Alice"], [2, "Bob"]]);
        var executor = new StoredProcedureExecutor(new SpToolkitOptions(), connection);

        var result = await executor.QueryAsync<EmptyRequest, UserRow>("dbo.GetUsers", EmptyRequest.Instance);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
    }

    [Fact]
    public async Task QueryAsync_binds_input_parameters_and_calls_ExecuteReader()
    {
        var connection = ConnectionWithRows(["Id", "Name"], [[7, "Grace"]]);
        var executor = new StoredProcedureExecutor(new SpToolkitOptions(), connection);
        var request = new GetUserRequest { Id = 7 };

        await executor.QueryAsync<GetUserRequest, UserRow>("dbo.GetUserById", request);

        var cmd = connection.LastCommand;
        Assert.NotNull(cmd);
        Assert.Equal(1, cmd.ExecuteReaderCallCount);
        Assert.Equal("dbo.GetUserById", cmd.CommandText);
        Assert.Equal(CommandType.StoredProcedure, cmd.CommandType);
    }

    [Fact]
    public async Task QueryAsync_result_is_read_only_list()
    {
        var connection = ConnectionWithRows(["Id", "Name"], [[1, "Alice"]]);
        var executor = new StoredProcedureExecutor(new SpToolkitOptions(), connection);

        var result = await executor.QueryAsync<EmptyRequest, UserRow>("dbo.GetUsers", EmptyRequest.Instance);

        Assert.IsAssignableFrom<IReadOnlyList<UserRow>>(result);
    }

    // ── QuerySingleAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task QuerySingleAsync_empty_result_returns_null()
    {
        var connection = ConnectionWithRows(["Id", "Name"], []);
        var executor = new StoredProcedureExecutor(new SpToolkitOptions(), connection);

        var result = await executor.QuerySingleAsync<EmptyRequest, UserRow>("dbo.GetUser", EmptyRequest.Instance);

        Assert.Null(result);
    }

    [Fact]
    public async Task QuerySingleAsync_returns_first_row()
    {
        var connection = ConnectionWithRows(
            ["Id", "Name"],
            [[42, "Henry"]]);
        var executor = new StoredProcedureExecutor(new SpToolkitOptions(), connection);

        var result = await executor.QuerySingleAsync<EmptyRequest, UserRow>("dbo.GetUser", EmptyRequest.Instance);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Henry", result.Name);
    }

    // ── QueryWithOutputsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task QueryWithOutputsAsync_returns_rows_and_output_parameters()
    {
        var connection = ConnectionWithRows(["Id", "Name"], [[1, "Alice"], [2, "Bob"]]);
        var executor = new StoredProcedureExecutor(new SpToolkitOptions(), connection);

        var result = await executor.QueryWithOutputsAsync<EmptyRequest, UserRow, UserCountOutput>(
            "dbo.GetPagedUsers", EmptyRequest.Instance);

        Assert.Equal(2, result.Data.Count);
        Assert.NotNull(result.Output);
    }

    // ── QuerySingleWithOutputsAsync ──────────────────────────────────────────

    [Fact]
    public async Task QuerySingleWithOutputsAsync_returns_single_row_and_output()
    {
        var connection = ConnectionWithRows(["Id", "Name"], [[99, "Ivy"]]);
        var executor = new StoredProcedureExecutor(new SpToolkitOptions(), connection);

        var result = await executor.QuerySingleWithOutputsAsync<EmptyRequest, UserRow, UserCountOutput>(
            "dbo.GetUserWithStatus", EmptyRequest.Instance);

        Assert.NotNull(result.Data);
        Assert.Equal(99, result.Data.Id);
        Assert.NotNull(result.Output);
    }

    [Fact]
    public async Task QuerySingleWithOutputsAsync_empty_result_returns_null_data_with_output()
    {
        var connection = ConnectionWithRows(["Id", "Name"], []);
        var executor = new StoredProcedureExecutor(new SpToolkitOptions(), connection);

        var result = await executor.QuerySingleWithOutputsAsync<EmptyRequest, UserRow, UserCountOutput>(
            "dbo.GetUserWithStatus", EmptyRequest.Instance);

        Assert.Null(result.Data);
        Assert.NotNull(result.Output);
    }
}
