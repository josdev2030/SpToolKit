using SpToolkit.Generator.Configuration;
using SpToolkit.Generator.Metadata;
using SpToolkit.Generator.Naming;
using SpToolkit.Generator.Output;
using SpToolkit.Generator.Resolved;
using SpToolkit.Generator.Templates;
using SpToolkit.Generator.TypeMapping;

namespace SpToolkit.Generator.Orchestration;

public sealed class GenerationOrchestrator
{
    private readonly MetadataReader _metadataReader;
    private readonly TypeMapper _typeMapper;
    private readonly NamingService _namingService;
    private readonly TemplateRenderer _templateRenderer;
    private readonly FileWriter _fileWriter;

    public GenerationOrchestrator(GeneratorOptions options)
    {
        _metadataReader   = new MetadataReader();
        _typeMapper       = new TypeMapper();
        _namingService    = new NamingService(options);
        _templateRenderer = new TemplateRenderer();
        _fileWriter       = new FileWriter();
    }

    /// <summary>
    /// Runs the full generation pipeline: reads metadata, resolves names and types,
    /// renders .g.cs files, writes them to disk, and returns a report.
    /// </summary>
    public async Task<GenerationReport> GenerateAsync(
        GeneratorOptions options,
        CancellationToken ct = default)
    {
        var allMetadata = await _metadataReader.ReadAllAsync(options, ct);

        var generatedFiles = new List<GeneratedFile>();
        var reportEntries  = new List<GenerationReportEntry>();

        // Pre-pass: separate excluded SPs from ones to resolve
        var toResolve = new List<StoredProcedureMetadata>(allMetadata.Count);
        foreach (var sp in allMetadata)
        {
            var ov = FindOverride(sp.FullName, options.Overrides);
            if (ov is { Exclude: true })
            {
                reportEntries.Add(new GenerationReportEntry
                {
                    ProcedureName  = sp.FullName,
                    Status         = "Excluded",
                    Message        = null,
                    FilesGenerated = [],
                });
            }
            else
            {
                toResolve.Add(sp);
            }
        }

        var resolvedProcedures = ResolveProcedures(toResolve, options);

        foreach (var proc in resolvedProcedures)
        {
            var filesForProc = new List<string>();
            string status    = "Success";
            string? message  = proc.Warning;

            if (proc.Warning is not null)
                status = "Warning";

            // Request -- generated only when the SP has input parameters.
            // For no-input SPs, no Request class is emitted; wrappers use EmptyRequest.Instance.
            if (proc.HasInputParameters)
            {
                var requestFile = new GeneratedFile
                {
                    FileName = $"{proc.RequestClassName}.g.cs",
                    Content  = _templateRenderer.RenderRequest(options.Namespace, proc),
                };
                generatedFiles.Add(requestFile);
                filesForProc.Add(requestFile.FileName);
            }

            // Response -- only when there are output parameters
            if (proc.ResponseClassName is not null && proc.OutputParameters.Count > 0)
            {
                var responseFile = new GeneratedFile
                {
                    FileName = $"{proc.ResponseClassName}.g.cs",
                    Content  = _templateRenderer.RenderResponse(options.Namespace, proc),
                };
                generatedFiles.Add(responseFile);
                filesForProc.Add(responseFile.FileName);
            }

            // Row -- only when there is an inferrable result set
            if (proc.RowClassName is not null && proc.ResultColumns is { Count: > 0 })
            {
                var rowFile = new GeneratedFile
                {
                    FileName = $"{proc.RowClassName}.g.cs",
                    Content  = _templateRenderer.RenderRow(options.Namespace, proc),
                };
                generatedFiles.Add(rowFile);
                filesForProc.Add(rowFile.FileName);
            }

            reportEntries.Add(new GenerationReportEntry
            {
                ProcedureName  = proc.FullName,
                Status         = status,
                Message        = message,
                FilesGenerated = filesForProc,
            });
        }

        // Wrapper -- one file for all non-excluded procedures
        var wrapperFile = new GeneratedFile
        {
            FileName = $"{options.WrapperClassName}.g.cs",
            Content  = _templateRenderer.RenderWrapper(
                options.Namespace, options.WrapperClassName, resolvedProcedures),
        };
        generatedFiles.Add(wrapperFile);

        await _fileWriter.WriteAsync(options.OutputDirectory, generatedFiles, ct);

        var report = BuildReport(options, reportEntries);

        await _fileWriter.WriteReportAsync(options.OutputDirectory, report, ct);

        return report;
    }

    private List<ResolvedProcedure> ResolveProcedures(
        IReadOnlyList<StoredProcedureMetadata> allMetadata,
        GeneratorOptions options)
    {
        // First pass: compute base names to detect collisions
        var baseNames = new Dictionary<string, List<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase);

        foreach (var sp in allMetadata)
        {
            var ov       = FindOverride(sp.FullName, options.Overrides);
            var baseName = ov?.BaseName ?? _namingService.ToClassName(sp.ProcedureName);

            if (!baseNames.TryGetValue(baseName, out var list))
            {
                list = [];
                baseNames[baseName] = list;
            }

            list.Add(sp);
        }

        // Second pass: resolve each SP with schema disambiguation where needed
        var resolved = new List<ResolvedProcedure>(allMetadata.Count);

        foreach (var sp in allMetadata)
        {
            var ov       = FindOverride(sp.FullName, options.Overrides);
            var baseName = ov?.BaseName ?? _namingService.ToClassName(sp.ProcedureName);

            // Disambiguate only for auto-named SPs (not overridden ones)
            if (ov?.BaseName is null && baseNames[baseName].Count > 1)
                baseName = _namingService.DisambiguateWithSchema(sp.SchemaName, baseName);

            var inputParams  = ResolveParameters(sp.Parameters.Where(p => !p.IsOutput));
            var outputParams = ResolveParameters(sp.Parameters.Where(p => p.IsOutput));

            // ResultColumns: prefer manual override, then inferred from metadata
            List<ResolvedColumn>? resultCols;
            if (ov?.ResultColumns is { Length: > 0 })
                resultCols = ParseManualResultColumns(ov.ResultColumns);
            else
                resultCols = sp.HasResultSet ? ResolveColumns(sp.ResultColumns!) : null;

            var effectiveHasResultSet = sp.HasResultSet || (resultCols is { Count: > 0 });
            var defaultPattern = ExecutionPatternRules.DetermineDefaultPattern(
                effectiveHasResultSet,
                outputParams.Count > 0);
            var pattern = ov?.ExecutionPattern ?? defaultPattern;

            ExecutionPatternRules.ValidateExecutionPattern(
                sp.FullName,
                pattern,
                effectiveHasResultSet,
                outputParams.Count > 0);

            // Determine class names
            var requestClassName  = baseName + "Request";
            var responseClassName = outputParams.Count > 0 ? baseName + "Response" : null;
            var rowClassName      = resultCols is { Count: > 0 } ? baseName + "Row" : null;

            // Warning: carry forward result set error, unless overridden with manual columns
            string? warning = (ov?.ResultColumns is null) ? sp.ResultSetError : null;

            // MethodName: prefer override, then auto-generate
            var methodName = ov?.MethodName ?? _namingService.ToMethodName(baseName);

            resolved.Add(new ResolvedProcedure
            {
                FullName           = sp.FullName,
                BaseName           = baseName,
                RequestClassName   = requestClassName,
                ResponseClassName  = responseClassName,
                RowClassName       = rowClassName,
                MethodName         = methodName,
                Pattern            = pattern,
                InputParameters    = inputParams,
                OutputParameters   = outputParams,
                ResultColumns      = resultCols,
                HasInputParameters = sp.HasInputParameters,
                Warning            = warning,
            });
        }

        return resolved;
    }

    private List<ResolvedParameter> ResolveParameters(IEnumerable<ParameterMetadata> parameters)
    {
        var result = new List<ResolvedParameter>();

        foreach (var p in parameters)
        {
            var mapping = _typeMapper.Map(p.SqlTypeName, isNullable: false);
            var size    = _typeMapper.ResolveSize(p.SqlTypeName, p.MaxLength);

            result.Add(new ResolvedParameter
            {
                PropertyName           = _namingService.ToPropertyName(p.Name),
                ClrTypeName            = mapping.ClrTypeName,
                SqlParameterName       = p.Name,
                SqlDbTypeName          = mapping.SqlDbTypeName,
                Size                   = size,
                Precision              = p.Precision,
                Scale                  = p.Scale,
                DefaultValueExpression = mapping.DefaultValueExpression,
            });
        }

        return result;
    }

    private List<ResolvedColumn> ResolveColumns(IReadOnlyList<ResultColumnMetadata> columns)
    {
        var result = new List<ResolvedColumn>(columns.Count);

        foreach (var col in columns)
        {
            var mapping = _typeMapper.Map(col.SqlTypeName, col.IsNullable);

            result.Add(new ResolvedColumn
            {
                PropertyName           = _namingService.ToPropertyName(col.Name),
                ClrTypeName            = mapping.ClrTypeName,
                ColumnName             = col.Name,
                DefaultValueExpression = mapping.DefaultValueExpression,
            });
        }

        return result;
    }

    /// <summary>
    /// Builds ResolvedColumn list from manual override entries like "UserId:int", "Name:string?".
    /// </summary>
    private List<ResolvedColumn> ParseManualResultColumns(string[] entries)
    {
        var result = new List<ResolvedColumn>(entries.Length);

        foreach (var entry in entries)
        {
            var sep = entry.LastIndexOf(':');
            if (sep <= 0) continue;

            var colName  = entry[..sep].Trim();
            var clrType  = entry[(sep + 1)..].Trim();
            var isNullable = clrType.EndsWith('?');

            result.Add(new ResolvedColumn
            {
                PropertyName           = _namingService.ToPropertyName(colName),
                ClrTypeName            = clrType,
                ColumnName             = colName,
                DefaultValueExpression = GetDefaultForManualType(clrType, isNullable),
            });
        }

        return result;
    }

    private static string? GetDefaultForManualType(string clrType, bool isNullable)
    {
        if (isNullable) return null;
        return clrType switch
        {
            "string"   => "string.Empty",
            "int"      => "0",
            "long"     => "0L",
            "decimal"  => "0m",
            "double"   => "0.0",
            "float"    => "0f",
            "bool"     => "false",
            "Guid"     => "Guid.Empty",
            "DateTime" => "default",
            "DateOnly" => "default",
            "TimeOnly" => "default",
            _          => null,
        };
    }

    /// <summary>
    /// Finds the first override whose Procedure pattern matches the given full SP name.
    /// Supports exact match and prefix wildcard (e.g. "dbo.SP_INTERNAL_*").
    /// </summary>
    private static ProcedureOverride? FindOverride(string fullName, ProcedureOverride[] overrides)
    {
        foreach (var ov in overrides)
        {
            if (ov.Procedure.EndsWith('*'))
            {
                var prefix = ov.Procedure[..^1];
                if (fullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return ov;
            }
            else if (string.Equals(fullName, ov.Procedure, StringComparison.OrdinalIgnoreCase))
            {
                return ov;
            }
        }

        return null;
    }

    private static GenerationReport BuildReport(
        GeneratorOptions options,
        List<GenerationReportEntry> entries)
    {
        int successCount = entries.Count(e => e.Status == "Success");
        int warningCount = entries.Count(e => e.Status == "Warning");
        int errorCount   = entries.Count(e => e.Status == "Error");
        int excluded     = entries.Count(e => e.Status == "Excluded");

        return new GenerationReport
        {
            Namespace        = options.Namespace,
            OutputDirectory  = options.OutputDirectory,
            TotalProcedures  = entries.Count,
            SuccessCount     = successCount,
            WarningCount     = warningCount,
            ExcludedCount    = excluded,
            ErrorCount       = errorCount,
            Entries          = entries,
        };
    }
}
