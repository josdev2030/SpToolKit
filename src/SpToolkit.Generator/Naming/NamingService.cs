using SpToolkit.Generator.Configuration;
using System.Text;

namespace SpToolkit.Generator.Naming;

public sealed class NamingService
{
    private readonly GeneratorOptions _options;

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate",
        "do", "double", "else", "enum", "event", "explicit", "extern", "false",
        "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
        "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private",
        "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
        // contextual keywords that can still cause confusion
        "var", "dynamic", "record", "init", "required", "file", "scoped",
    };

    public NamingService(GeneratorOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Converts a stored procedure name to a PascalCase C# class base name,
    /// removing configured prefixes.
    /// Does NOT handle schema collision disambiguation — the orchestrator
    /// calls <see cref="DisambiguateWithSchema"/> when needed.
    /// </summary>
    public string ToClassName(string procedureName)
    {
        var name = procedureName;

        foreach (var prefix in _options.PrefixesToRemove)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name[prefix.Length..];
                break;
            }
        }

        return EnsureValidIdentifier(SnakeToPascal(name));
    }

    /// <summary>
    /// Prepends the schema name in PascalCase to a base name to resolve collisions.
    /// </summary>
    public string DisambiguateWithSchema(string schemaName, string baseName)
        => EnsureValidIdentifier(SnakeToPascal(schemaName) + baseName);

    /// <summary>
    /// Converts a SQL parameter or column name to a PascalCase C# property name.
    /// Strips leading @ if present.
    /// </summary>
    public string ToPropertyName(string sqlName)
    {
        var name = sqlName.TrimStart('@');
        return EnsureValidIdentifier(SnakeToPascal(name));
    }

    /// <summary>Returns baseName + "Async".</summary>
    public string ToMethodName(string baseName) => baseName + "Async";

    /// <summary>
    /// Ensures a name is a valid C# identifier.
    /// Escapes reserved keywords with @, prefixes digit-starting names with _,
    /// and strips non-identifier characters.
    /// </summary>
    public string EnsureValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "_";

        var sb = new StringBuilder(name.Length + 1);

        foreach (var ch in name)
        {
            if (sb.Length == 0)
            {
                if (char.IsLetter(ch) || ch == '_')
                    sb.Append(ch);
                else if (char.IsDigit(ch))
                {
                    sb.Append('_');
                    sb.Append(ch);
                }
                // else: skip invalid leading characters
            }
            else
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    sb.Append(ch);
                // else: skip invalid characters
            }
        }

        if (sb.Length == 0)
            return "_";

        var result = sb.ToString();

        if (CSharpKeywords.Contains(result))
            return "@" + result;

        return result;
    }

    /// <summary>
    /// Converts SNAKE_CASE or ALLCAPS to PascalCase.
    /// Single-word names with no underscore are title-cased (first letter upper, rest lower).
    /// </summary>
    private static string SnakeToPascal(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var segments = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder(name.Length);

        foreach (var segment in segments)
        {
            if (segment.Length == 0)
                continue;

            sb.Append(char.ToUpperInvariant(segment[0]));

            for (int i = 1; i < segment.Length; i++)
                sb.Append(char.ToLowerInvariant(segment[i]));
        }

        return sb.ToString();
    }
}
