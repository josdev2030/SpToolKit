using System.Data;
using SpToolkit.Abstractions.Exceptions;
using SpToolkit.Runtime.TypeConversion;
using Xunit;

namespace SpToolkit.Runtime.Tests;

public sealed class TypeConversionServiceTests
{
    private readonly TypeConversionService _sut = new();

    // ── ToSqlValue ──────────────────────────────────────────────────────────

    [Fact]
    public void ToSqlValue_null_returns_DBNull()
    {
        var result = _sut.ToSqlValue(null);
        Assert.Equal(DBNull.Value, result);
    }

    [Fact]
    public void ToSqlValue_non_null_value_returned_as_is()
    {
        var result = _sut.ToSqlValue(42);
        Assert.Equal(42, result);
    }

    [Fact]
    public void ToSqlValue_string_returned_as_is()
    {
        var result = _sut.ToSqlValue("hello");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void ToSqlValue_enum_returns_underlying_integer()
    {
        var result = _sut.ToSqlValue(DayOfWeek.Wednesday); // underlying value = 3
        Assert.Equal(3, result);
    }

    [Fact]
    public void ToSqlValue_enum_with_long_underlying_type_returns_long()
    {
        var result = _sut.ToSqlValue(LongEnum.BigValue);
        Assert.Equal((long)LongEnum.BigValue, result);
    }

    // ── FromSqlValue ────────────────────────────────────────────────────────

    [Fact]
    public void FromSqlValue_DBNull_to_nullable_int_returns_null()
    {
        var result = _sut.FromSqlValue(DBNull.Value, typeof(int?));
        Assert.Null(result);
    }

    [Fact]
    public void FromSqlValue_DBNull_to_nullable_string_returns_null()
    {
        var result = _sut.FromSqlValue(DBNull.Value, typeof(string));
        Assert.Null(result);
    }

    [Fact]
    public void FromSqlValue_DBNull_to_non_nullable_value_type_throws_SpMappingException()
    {
        var ex = Assert.Throws<SpMappingException>(() =>
            _sut.FromSqlValue(DBNull.Value, typeof(int)));
        Assert.Contains("NULL", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromSqlValue_same_type_returns_value_unchanged()
    {
        var result = _sut.FromSqlValue(123, typeof(int));
        Assert.Equal(123, result);
    }

    [Fact]
    public void FromSqlValue_int_to_nullable_int_returns_value()
    {
        var result = _sut.FromSqlValue(42, typeof(int?));
        Assert.Equal(42, result);
    }

    [Fact]
    public void FromSqlValue_int_to_enum_returns_enum_value()
    {
        var result = _sut.FromSqlValue(3, typeof(DayOfWeek));
        Assert.Equal(DayOfWeek.Wednesday, result);
    }

    [Fact]
    public void FromSqlValue_int_to_nullable_enum_returns_enum_value()
    {
        var result = _sut.FromSqlValue(1, typeof(DayOfWeek?));
        Assert.Equal(DayOfWeek.Monday, result);
    }

    [Fact]
    public void FromSqlValue_DateTime_to_DateOnly_extracts_date_part()
    {
        var dt = new DateTime(2024, 6, 15, 10, 30, 0);
        var result = _sut.FromSqlValue(dt, typeof(DateOnly));
        Assert.Equal(new DateOnly(2024, 6, 15), result);
    }

    [Fact]
    public void FromSqlValue_DateTime_to_nullable_DateOnly_extracts_date_part()
    {
        var dt = new DateTime(2024, 6, 15);
        var result = _sut.FromSqlValue(dt, typeof(DateOnly?));
        Assert.Equal(new DateOnly(2024, 6, 15), result);
    }

    [Fact]
    public void FromSqlValue_TimeSpan_to_TimeOnly_converts_correctly()
    {
        var ts = new TimeSpan(14, 30, 0);
        var result = _sut.FromSqlValue(ts, typeof(TimeOnly));
        Assert.Equal(new TimeOnly(14, 30, 0), result);
    }

    [Fact]
    public void FromSqlValue_long_to_int_converts_via_ChangeType()
    {
        var result = _sut.FromSqlValue(100L, typeof(int));
        Assert.Equal(100, result);
    }

    [Fact]
    public void FromSqlValue_incompatible_type_throws_SpMappingException()
    {
        var ex = Assert.Throws<SpMappingException>(() =>
            _sut.FromSqlValue("not-a-guid", typeof(Guid)));
        Assert.Contains("Cannot convert", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── InferSqlDbType ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(typeof(string), SqlDbType.NVarChar)]
    [InlineData(typeof(int), SqlDbType.Int)]
    [InlineData(typeof(long), SqlDbType.BigInt)]
    [InlineData(typeof(short), SqlDbType.SmallInt)]
    [InlineData(typeof(byte), SqlDbType.TinyInt)]
    [InlineData(typeof(bool), SqlDbType.Bit)]
    [InlineData(typeof(decimal), SqlDbType.Decimal)]
    [InlineData(typeof(double), SqlDbType.Float)]
    [InlineData(typeof(float), SqlDbType.Real)]
    [InlineData(typeof(DateTime), SqlDbType.DateTime2)]
    [InlineData(typeof(DateOnly), SqlDbType.Date)]
    [InlineData(typeof(TimeOnly), SqlDbType.Time)]
    [InlineData(typeof(Guid), SqlDbType.UniqueIdentifier)]
    [InlineData(typeof(byte[]), SqlDbType.VarBinary)]
    public void InferSqlDbType_maps_clr_type_to_expected_sql_type(Type clrType, SqlDbType expected)
    {
        var result = _sut.InferSqlDbType(clrType);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void InferSqlDbType_nullable_int_returns_Int()
    {
        var result = _sut.InferSqlDbType(typeof(int?));
        Assert.Equal(SqlDbType.Int, result);
    }

    [Fact]
    public void InferSqlDbType_enum_returns_underlying_type_mapping()
    {
        // DayOfWeek has underlying type int
        var result = _sut.InferSqlDbType(typeof(DayOfWeek));
        Assert.Equal(SqlDbType.Int, result);
    }

    [Fact]
    public void InferSqlDbType_unknown_type_throws_SpMappingException()
    {
        var ex = Assert.Throws<SpMappingException>(() =>
            _sut.InferSqlDbType(typeof(TypeConversionServiceTests)));
        Assert.Contains("SqlDbType", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private enum LongEnum : long { BigValue = long.MaxValue }
}
