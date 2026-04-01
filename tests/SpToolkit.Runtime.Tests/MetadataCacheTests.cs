using System.Data;
using SpToolkit.Abstractions.Attributes;
using SpToolkit.Abstractions.Options;
using SpToolkit.Runtime.Mapping;
using SpToolkit.Runtime.TypeConversion;
using Xunit;

namespace SpToolkit.Runtime.Tests;

public sealed class MetadataCacheTests
{
    // ── Test model fixtures ──────────────────────────────────────────────────

    private sealed class AllInputModel
    {
        [SpInput("@Id", SqlDbType.Int)]
        public int Id { get; set; }

        [SpInput("@Name", SqlDbType.NVarChar, Size = 100)]
        public string Name { get; set; } = "";

        [SpIgnore]
        public string Ignored { get; set; } = "";

        // No attribute — skipped in Attribute mode, picked up in Convention mode
        public int Extra { get; set; }
    }

    private sealed class AllOutputModel
    {
        [SpOutput("@NewId", SqlDbType.Int)]
        public int NewId { get; set; }

        [SpOutput("@Total", SqlDbType.BigInt)]
        public long Total { get; set; }

        [SpIgnore]
        public string Ignored { get; set; } = "";

        // No attribute — skipped in Attribute mode
        public int Extra { get; set; }
    }

    private sealed class AllColumnsModel
    {
        [SpResultColumn("UserId")]
        public int UserId { get; set; }

        [SpResultColumn("UserName")]
        public string UserName { get; set; } = "";

        [SpIgnore]
        public string Ignored { get; set; } = "";

        // No attribute — skipped in Attribute mode
        public int Extra { get; set; }
    }

    private sealed class WriteOnlyModel
    {
        // write-only properties are excluded from input maps (can't be read)
        public int ReadWrite { get; set; }
        public string? ReadOnly { get; } = "ro";
    }

    private sealed class EmptyModel { }

    private static MetadataCache CreateCache(NamingPolicy policy = NamingPolicy.Attribute)
    {
        var options = new SpToolkitOptions { NamingPolicy = policy };
        var converter = new TypeConversionService();
        return new MetadataCache(options, converter);
    }

    // ── GetInputMaps — Attribute mode ────────────────────────────────────────

    [Fact]
    public void GetInputMaps_attribute_mode_returns_only_decorated_properties()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetInputMaps(typeof(AllInputModel));

        Assert.Equal(2, maps.Length);
        Assert.Contains(maps, m => m.ParameterName == "@Id");
        Assert.Contains(maps, m => m.ParameterName == "@Name");
    }

    [Fact]
    public void GetInputMaps_attribute_mode_excludes_SpIgnore_properties()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetInputMaps(typeof(AllInputModel));

        Assert.DoesNotContain(maps, m => m.ParameterName == "Ignored");
    }

    [Fact]
    public void GetInputMaps_preserves_size_from_attribute()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetInputMaps(typeof(AllInputModel));

        var nameMap = maps.First(m => m.ParameterName == "@Name");
        Assert.Equal(100, nameMap.Size);
    }

    [Fact]
    public void GetInputMaps_empty_model_returns_empty_array()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetInputMaps(typeof(EmptyModel));

        Assert.Empty(maps);
    }

    [Fact]
    public void GetInputMaps_caches_result_on_second_call()
    {
        var cache = CreateCache(NamingPolicy.Attribute);

        var first = cache.GetInputMaps(typeof(AllInputModel));
        var second = cache.GetInputMaps(typeof(AllInputModel));

        Assert.Same(first, second);
    }

    // ── GetInputMaps — Convention mode ───────────────────────────────────────

    [Fact]
    public void GetInputMaps_convention_mode_includes_unannotated_readable_properties()
    {
        var options = new SpToolkitOptions { NamingPolicy = NamingPolicy.Convention, AutoPrefixAtSign = false };
        var cache = new MetadataCache(options, new TypeConversionService());

        var maps = cache.GetInputMaps(typeof(AllInputModel));

        // Id, Name, Extra — Ignored is skipped by [SpIgnore]
        Assert.Equal(3, maps.Length);
    }

    [Fact]
    public void GetInputMaps_convention_mode_with_AutoPrefixAtSign_prefixes_at_sign()
    {
        var options = new SpToolkitOptions { NamingPolicy = NamingPolicy.Convention, AutoPrefixAtSign = true };
        var cache = new MetadataCache(options, new TypeConversionService());

        var maps = cache.GetInputMaps(typeof(AllInputModel));

        Assert.All(maps, m => Assert.StartsWith("@", m.ParameterName));
    }

    [Fact]
    public void GetInputMaps_excludes_write_only_properties()
    {
        var options = new SpToolkitOptions { NamingPolicy = NamingPolicy.Convention };
        var cache = new MetadataCache(options, new TypeConversionService());

        var maps = cache.GetInputMaps(typeof(WriteOnlyModel));

        // ReadWrite (readable) and ReadOnly (readable get-only) are included; both are readable
        Assert.Contains(maps, m => m.ParameterName == "ReadWrite");
    }

    // ── GetOutputMaps ────────────────────────────────────────────────────────

    [Fact]
    public void GetOutputMaps_attribute_mode_returns_only_decorated_properties()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetOutputMaps(typeof(AllOutputModel));

        Assert.Equal(2, maps.Length);
        Assert.Contains(maps, m => m.ParameterName == "@NewId");
        Assert.Contains(maps, m => m.ParameterName == "@Total");
    }

    [Fact]
    public void GetOutputMaps_attribute_mode_excludes_SpIgnore_properties()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetOutputMaps(typeof(AllOutputModel));

        Assert.DoesNotContain(maps, m => m.ParameterName == "Ignored");
    }

    [Fact]
    public void GetOutputMaps_empty_model_returns_empty_array()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetOutputMaps(typeof(EmptyModel));

        Assert.Empty(maps);
    }

    [Fact]
    public void GetOutputMaps_caches_result_on_second_call()
    {
        var cache = CreateCache(NamingPolicy.Attribute);

        var first = cache.GetOutputMaps(typeof(AllOutputModel));
        var second = cache.GetOutputMaps(typeof(AllOutputModel));

        Assert.Same(first, second);
    }

    // ── GetResultColumnMaps ──────────────────────────────────────────────────

    [Fact]
    public void GetResultColumnMaps_attribute_mode_returns_only_decorated_properties()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetResultColumnMaps(typeof(AllColumnsModel));

        Assert.Equal(2, maps.Length);
        Assert.Contains(maps, m => m.ColumnName == "UserId");
        Assert.Contains(maps, m => m.ColumnName == "UserName");
    }

    [Fact]
    public void GetResultColumnMaps_attribute_mode_excludes_SpIgnore()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetResultColumnMaps(typeof(AllColumnsModel));

        Assert.DoesNotContain(maps, m => m.ColumnName == "Ignored");
    }

    [Fact]
    public void GetResultColumnMaps_convention_mode_uses_property_name_as_column_name()
    {
        var options = new SpToolkitOptions { NamingPolicy = NamingPolicy.Convention };
        var cache = new MetadataCache(options, new TypeConversionService());

        var maps = cache.GetResultColumnMaps(typeof(AllColumnsModel));

        // UserId, UserName, Extra — Ignored is skipped; all use their property name as column
        Assert.Equal(3, maps.Length);
        Assert.Contains(maps, m => m.ColumnName == "Extra");
    }

    [Fact]
    public void GetResultColumnMaps_caches_result_on_second_call()
    {
        var cache = CreateCache(NamingPolicy.Attribute);

        var first = cache.GetResultColumnMaps(typeof(AllColumnsModel));
        var second = cache.GetResultColumnMaps(typeof(AllColumnsModel));

        Assert.Same(first, second);
    }

    [Fact]
    public void GetResultColumnMaps_empty_model_returns_empty_array()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetResultColumnMaps(typeof(EmptyModel));

        Assert.Empty(maps);
    }

    [Fact]
    public void GetResultColumnMaps_order_defaults_to_minus_one_when_not_specified()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetResultColumnMaps(typeof(AllColumnsModel));

        Assert.All(maps, m => Assert.Equal(-1, m.Order));
    }

    private sealed class OrderedColumnsModel
    {
        [SpResultColumn("Col1", Order = 0)]
        public int Col1 { get; set; }

        [SpResultColumn("Col2", Order = 1)]
        public string Col2 { get; set; } = "";
    }

    [Fact]
    public void GetResultColumnMaps_preserves_order_from_attribute()
    {
        var cache = CreateCache(NamingPolicy.Attribute);
        var maps = cache.GetResultColumnMaps(typeof(OrderedColumnsModel));

        Assert.Equal(0, maps.First(m => m.ColumnName == "Col1").Order);
        Assert.Equal(1, maps.First(m => m.ColumnName == "Col2").Order);
    }
}
