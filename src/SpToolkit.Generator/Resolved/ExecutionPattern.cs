namespace SpToolkit.Generator.Resolved;

public enum ExecutionPattern
{
    /// <summary>SP has output parameters but no result set. Uses ExecuteAsync.</summary>
    ExecuteOnly,

    /// <summary>SP has a result set but no output parameters. Uses QueryAsync.</summary>
    Query,

    /// <summary>SP has both a result set and output parameters. Uses QueryWithOutputsAsync.</summary>
    QueryWithOutputs,
}
