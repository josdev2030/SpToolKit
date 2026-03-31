using SpToolkit.Abstractions.Attributes;
using SpToolkit.Abstractions.Exceptions;
using SpToolkit.Abstractions.Options;
using SpToolkit.Runtime.Mapping;
using SpToolkit.Runtime.TypeConversion;
using Xunit;

namespace SpToolkit.Runtime.Tests;

public sealed class RowMaterializerTests
{
    // ── Test row fixtures ────────────────────────────────────────────────────

    private sealed class UserRow
    {
        [SpResultColumn("UserId")]
        public int UserId { get; set; }

        [SpResultColumn("UserName")]
        public string UserName { get; set; } = "";

        [SpResultColumn("IsActive")]
        public bool IsActive { get; set; }
    }

    private sealed class NullableRow
    {
        [SpResultColumn("Value")]
        public int? Value { get; set; }
    }

    private sealed class OrderedRow
    {
        [SpResultColumn("Col1", Order = 0)]
        public int Col1 { get; set; }

        [SpResultColumn("Col2", Order = 1)]
        public string Col2 { get; set; } = "";
    }

    private static RowMaterializer CreateMaterializer(
        MissingColumnBehavior missingBehavior = MissingColumnBehavior.Ignore,
        bool caseSensitive = false)
    {
        var options = new SpToolkitOptions
        {
            NamingPolicy = NamingPolicy.Attribute,
            MissingColumnBehavior = missingBehavior,
            CaseSensitiveColumnMatching = caseSensitive
        };
        var converter = new TypeConversionService();
        var cache = new MetadataCache(options, converter);
        return new RowMaterializer(cache, converter, options);
    }

    // ── MaterializeListAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task MaterializeListAsync_empty_reader_returns_empty_list()
    {
        var reader = new FakeDbDataReader(
            ["UserId", "UserName", "IsActive"],
            []);

        var materializer = CreateMaterializer();
        var result = await materializer.MaterializeListAsync<UserRow>(reader, default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task MaterializeListAsync_single_row_maps_all_columns()
    {
        var reader = new FakeDbDataReader(
            ["UserId", "UserName", "IsActive"],
            [[1, "Alice", true]]);

        var materializer = CreateMaterializer();
        var result = await materializer.MaterializeListAsync<UserRow>(reader, default);

        Assert.Single(result);
        var row = result[0];
        Assert.Equal(1, row.UserId);
        Assert.Equal("Alice", row.UserName);
        Assert.True(row.IsActive);
    }

    [Fact]
    public async Task MaterializeListAsync_multiple_rows_returns_all()
    {
        var reader = new FakeDbDataReader(
            ["UserId", "UserName", "IsActive"],
            [
                [1, "Alice", true],
                [2, "Bob", false],
                [3, "Carol", true]
            ]);

        var materializer = CreateMaterializer();
        var result = await materializer.MaterializeListAsync<UserRow>(reader, default);

        Assert.Equal(3, result.Count);
        Assert.Equal("Bob", result[1].UserName);
    }

    [Fact]
    public async Task MaterializeListAsync_column_name_matching_is_case_insensitive_by_default()
    {
        var reader = new FakeDbDataReader(
            ["USERID", "USERNAME", "ISACTIVE"],   // uppercase column names
            [[42, "Dave", false]]);

        var materializer = CreateMaterializer(caseSensitive: false);
        var result = await materializer.MaterializeListAsync<UserRow>(reader, default);

        Assert.Single(result);
        Assert.Equal(42, result[0].UserId);
    }

    [Fact]
    public async Task MaterializeListAsync_column_name_matching_is_case_sensitive_when_configured()
    {
        var reader = new FakeDbDataReader(
            ["USERID", "USERNAME", "ISACTIVE"],   // uppercase — won't match "UserId"
            [[99, "Eve", true]]);

        var materializer = CreateMaterializer(caseSensitive: true);
        var result = await materializer.MaterializeListAsync<UserRow>(reader, default);

        // Case-sensitive mismatch → properties stay at default (0, "", false) when Ignore
        Assert.Single(result);
        Assert.Equal(0, result[0].UserId);
    }

    [Fact]
    public async Task MaterializeListAsync_missing_column_ignored_by_default()
    {
        // Reader has no IsActive column
        var reader = new FakeDbDataReader(
            ["UserId", "UserName"],
            [[5, "Frank"]]);

        var materializer = CreateMaterializer(MissingColumnBehavior.Ignore);
        var result = await materializer.MaterializeListAsync<UserRow>(reader, default);

        Assert.Single(result);
        Assert.Equal(5, result[0].UserId);
        Assert.False(result[0].IsActive); // default
    }

    [Fact]
    public async Task MaterializeListAsync_missing_column_throws_when_configured()
    {
        var reader = new FakeDbDataReader(
            ["UserId", "UserName"],  // no IsActive
            [[5, "Frank"]]);

        var materializer = CreateMaterializer(MissingColumnBehavior.Throw);

        var ex = await Assert.ThrowsAsync<SpMappingException>(() =>
            materializer.MaterializeListAsync<UserRow>(reader, default));

        Assert.Contains("IsActive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MaterializeListAsync_nullable_column_with_DBNull_maps_to_null()
    {
        var reader = new FakeDbDataReader(
            ["Value"],
            [[null]]);

        var materializer = CreateMaterializer();
        var result = await materializer.MaterializeListAsync<NullableRow>(reader, default);

        Assert.Single(result);
        Assert.Null(result[0].Value);
    }

    [Fact]
    public async Task MaterializeListAsync_order_based_mapping_uses_ordinal_position()
    {
        // Columns named differently from what the attribute expects, but order wins
        var reader = new FakeDbDataReader(
            ["Anything", "Whatever"],
            [[77, "Ordered"]]);

        var materializer = CreateMaterializer();
        var result = await materializer.MaterializeListAsync<OrderedRow>(reader, default);

        Assert.Single(result);
        Assert.Equal(77, result[0].Col1);
        Assert.Equal("Ordered", result[0].Col2);
    }

    // ── MaterializeSingleAsync ───────────────────────────────────────────────

    [Fact]
    public async Task MaterializeSingleAsync_empty_reader_returns_null()
    {
        var reader = new FakeDbDataReader(
            ["UserId", "UserName", "IsActive"],
            []);

        var materializer = CreateMaterializer();
        var result = await materializer.MaterializeSingleAsync<UserRow>(reader, default);

        Assert.Null(result);
    }

    [Fact]
    public async Task MaterializeSingleAsync_returns_first_row_only()
    {
        var reader = new FakeDbDataReader(
            ["UserId", "UserName", "IsActive"],
            [
                [1, "Alice", true],
                [2, "Bob", false]
            ]);

        var materializer = CreateMaterializer();
        var result = await materializer.MaterializeSingleAsync<UserRow>(reader, default);

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal("Alice", result.UserName);
    }

    [Fact]
    public async Task MaterializeSingleAsync_single_row_maps_all_columns()
    {
        var reader = new FakeDbDataReader(
            ["UserId", "UserName", "IsActive"],
            [[7, "Grace", true]]);

        var materializer = CreateMaterializer();
        var result = await materializer.MaterializeSingleAsync<UserRow>(reader, default);

        Assert.NotNull(result);
        Assert.Equal(7, result.UserId);
        Assert.Equal("Grace", result.UserName);
        Assert.True(result.IsActive);
    }
}
