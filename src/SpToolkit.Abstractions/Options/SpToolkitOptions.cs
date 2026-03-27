namespace SpToolkit.Abstractions.Options;

/// <summary>
/// Global runtime configuration. Register via DI and inject where needed.
/// </summary>
public sealed class SpToolkitOptions
{
    /// <summary>
    /// Default connection string. May be null if the caller always supplies an external connection.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// How the runtime resolves parameter and column names.
    /// Default: Attribute (only maps properties decorated with Sp* attributes).
    /// </summary>
    public NamingPolicy NamingPolicy { get; set; } = NamingPolicy.Attribute;

    /// <summary>
    /// When false (default), column name matching is case-insensitive.
    /// </summary>
    public bool CaseSensitiveColumnMatching { get; set; } = false;

    /// <summary>
    /// What to do when a model property has no matching column or parameter in the reader/command.
    /// Default: Ignore (leave property at its default value).
    /// </summary>
    public MissingColumnBehavior MissingColumnBehavior { get; set; } = MissingColumnBehavior.Ignore;

    /// <summary>
    /// When true, the runtime prepends '@' to parameter names that don't already have it.
    /// Useful for convention-based mapping without explicit attributes.
    /// </summary>
    public bool AutoPrefixAtSign { get; set; } = false;

    /// <summary>
    /// Default size for string parameters (NVarChar/VarChar) when Size is 0 on the attribute.
    /// -1 means MAX. Default: -1.
    /// </summary>
    public int DefaultStringSize { get; set; } = -1;

    /// <summary>
    /// Enables or disables ILogger integration globally.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// When false (default), parameter values are not included in log output for security.
    /// </summary>
    public bool LogParameterValues { get; set; } = false;
}
