namespace SpToolkit.Generator.Resolved;

/// <summary>
/// Default pattern selection and validation for <see cref="ExecutionPattern"/> against procedure shape.
/// </summary>
public static class ExecutionPatternRules
{
    public static ExecutionPattern DetermineDefaultPattern(bool effectiveHasResultSet, bool hasOutputParameters)
    {
        if (effectiveHasResultSet && hasOutputParameters)
            return ExecutionPattern.QueryWithOutputs;

        if (effectiveHasResultSet)
            return ExecutionPattern.Query;

        return ExecutionPattern.ExecuteOnly;
    }

    /// <summary>
    /// Validates that the resolved pattern is compatible with effective result-set and output-parameter shape.
    /// </summary>
    /// <exception cref="InvalidOperationException">When the pattern does not match the procedure shape.</exception>
    public static void ValidateExecutionPattern(
        string fullName,
        ExecutionPattern pattern,
        bool effectiveHasResultSet,
        bool hasOutputParameters)
    {
        bool patternNeedsResultSet = pattern is
            ExecutionPattern.Query or
            ExecutionPattern.QuerySingle or
            ExecutionPattern.QueryWithOutputs or
            ExecutionPattern.QuerySingleWithOutputs;

        bool patternNeedsOutputs = pattern is
            ExecutionPattern.QueryWithOutputs or
            ExecutionPattern.QuerySingleWithOutputs;

        if (patternNeedsResultSet && !effectiveHasResultSet)
        {
            throw new InvalidOperationException(
                $"[{fullName}] Execution pattern '{pattern}' requires a result set, " +
                "but the procedure has none (no inferred columns and no manual ResultColumns). " +
                "Use 'ResultColumns' in the override if the SP uses dynamic SQL, " +
                "or choose 'ExecuteOnly' instead.");
        }

        if (!patternNeedsResultSet && effectiveHasResultSet)
        {
            throw new InvalidOperationException(
                $"[{fullName}] Execution pattern '{pattern}' does not consume a result set, " +
                "but the procedure returns rows. " +
                "Choose 'Query', 'QuerySingle', 'QueryWithOutputs', or 'QuerySingleWithOutputs' instead.");
        }

        if (patternNeedsOutputs && !hasOutputParameters)
        {
            throw new InvalidOperationException(
                $"[{fullName}] Execution pattern '{pattern}' requires output parameters, " +
                "but the procedure has none. " +
                "Use 'Query' or 'QuerySingle' instead.");
        }

        if (!patternNeedsOutputs && hasOutputParameters && patternNeedsResultSet)
        {
            throw new InvalidOperationException(
                $"[{fullName}] Execution pattern '{pattern}' does not consume output parameters, " +
                "but the procedure has some. " +
                "Use 'QueryWithOutputs' or 'QuerySingleWithOutputs' instead.");
        }
    }
}
