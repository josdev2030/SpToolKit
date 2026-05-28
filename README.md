# SpToolKit

SpToolKit generates strongly-typed C# wrappers around SQL Server stored procedures. You point the CLI tool at your database, it reads the procedure metadata, and emits `.g.cs` files with request, response, and row classes ready to use. At runtime, inject `IStoredProcedureExecutor` and call the generated methods — no raw ADO.NET in your application code.

## How it works

```
SQL Server stored procedures
        │
        ▼
  sp-generate (CLI)          ← reads metadata from your database
        │
        ▼
  Generated .g.cs files      ← request/response/row classes + wrapper
        │
        ▼
  Your application code      ← call wrapper methods via IStoredProcedureExecutor
```

## Requirements

- **.NET 10** (all packages target `net10.0`)
- **SQL Server** (metadata and execution are T-SQL specific)

## Installation

Install the runtime packages into your application project:

```bash
dotnet add package SpToolkit.Abstractions --version 0.1.0
dotnet add package SpToolkit.Runtime      --version 0.1.0
```

Install the CLI tool globally to generate code from your database:

```bash
dotnet tool install --global SpToolkit.Generator.Cli --version 0.1.0
```

| Package | What it contains | Who needs it |
|---------|-----------------|--------------|
| `SpToolkit.Abstractions` | Attributes, contracts, models used by generated code | Your application project |
| `SpToolkit.Runtime` | `StoredProcedureExecutor`, `AddSpToolkit` DI extension | Your application project |
| `SpToolkit.Generator.Cli` | `sp-generate` global tool | Developer machine / CI |

> For prerelease versions add `--prerelease` or pass the explicit `--version` string.

## Getting Started

### 1. Create the configuration file

Copy the [example config](sptoolkit.example.jsonc) to `sptoolkit.json` in your project root and fill in the three required fields:

```jsonc
{
  "ConnectionString": "Server=localhost;Database=MyDb;User Id=...;Password=...;TrustServerCertificate=True",
  "Namespace":        "MyProject.Data.StoredProcedures",
  "OutputDirectory":  "Generated/StoredProcedures"
}
```

See [Configuration options](#configuration-options) for the full list of settings.

### 2. Run the generator

```bash
sp-generate --config sptoolkit.json
```

The tool connects to your database, reads stored procedure signatures, and writes `.g.cs` files to `OutputDirectory`. Use `--dry-run` to preview the resolved configuration without touching your database or disk.

**What gets generated** — for a procedure `dbo.SP_GET_USERS` you get:

```
Generated/StoredProcedures/
  GetUsersRequest.g.cs      ← input parameters
  UserRow.g.cs              ← result set columns
  AppStoredProcedures.g.cs  ← wrapper class with GetUsersAsync method
```

### 3. Register in DI

In your application startup:

```csharp
// Option A: own connection string
services.AddSpToolkit(opts =>
{
    opts.ConnectionString = configuration.GetConnectionString("Default");
});

// Option B: reuse an existing EF Core DbContext connection
services.AddDbContext<AppDbContext>(...);
services.AddSpToolkit<AppDbContext>();
```

### 4. Call generated methods

Inject the generated wrapper (or `IStoredProcedureExecutor` directly) and call the methods:

```csharp
public class UserService
{
    private readonly IStoredProcedureExecutor _sp;

    public UserService(IStoredProcedureExecutor sp) => _sp = sp;

    // Query a result set
    public async Task<IReadOnlyList<UserRow>> GetUsersAsync(int maxRows)
        => await _sp.QueryAsync<GetUsersRequest, UserRow>(
               "dbo.SP_GET_USERS",
               new GetUsersRequest { MaxRows = maxRows });

    // Execute with output parameters
    public async Task<CreateUserResponse> CreateUserAsync(string name, string email)
        => await _sp.ExecuteAsync<CreateUserRequest, CreateUserResponse>(
               "dbo.SP_CREATE_USER",
               new CreateUserRequest { Name = name, Email = email });
}
```

## Execution patterns

The generator picks the right method based on what the stored procedure returns. You can also force a pattern per-procedure in `Overrides`.

| Pattern | Method | Use when |
|---------|--------|----------|
| Execute only | `ExecuteAsync<TInput>()` | No result set, no output parameters |
| Execute with outputs | `ExecuteAsync<TInput, TOutput>()` | No result set, has output parameters |
| Query | `QueryAsync<TInput, TRow>()` | Returns a result set, no output parameters |
| Query single | `QuerySingleAsync<TInput, TRow>()` | Returns 0 or 1 row, no output parameters |
| Query with outputs | `QueryWithOutputsAsync<TInput, TRow, TOutput>()` | Result set + output parameters |
| Query single with outputs | `QuerySingleWithOutputsAsync<TInput, TRow, TOutput>()` | 0 or 1 row + output parameters |

`QuerySingle` variants return `TRow?` (null when the SP returns no rows).  
`WithOutputs` variants return `SpResult<TData, TOutput>` with `.Data` and `.Output` properties.

## Configuration options

All options for `sptoolkit.json` with inline documentation are in the example template:

**[sptoolkit.example.jsonc](sptoolkit.example.jsonc)**

Quick reference:

| Option | Required | Default | Description |
|--------|----------|---------|-------------|
| `ConnectionString` | Yes* | — | SQL Server connection string |
| `Namespace` | Yes* | — | Namespace for generated classes |
| `OutputDirectory` | Yes* | — | Folder where `.g.cs` files are written |
| `Schemas` | No | `["dbo"]` | SQL schemas to inspect |
| `PrefixesToRemove` | No | `["SP_", "USP_"]` | Name prefixes stripped when deriving C# identifiers |
| `ExcludeProcedures` | No | `[]` | Procedure names to skip |
| `WrapperClassName` | No | `"AppStoredProcedures"` | Name of the generated wrapper class |
| `CaseSensitiveColumns` | No | `false` | Case-sensitive column name matching at runtime |
| `Overrides` | No | `[]` | Per-procedure rules: exclude, rename, manual columns, force pattern |

*Required unless the equivalent CLI flag is passed (`--connection`, `--namespace`, `--output`).

CLI flags take precedence over the config file:

```bash
sp-generate --config sptoolkit.json --connection "Server=...;" --output ./Generated
```

### Handling dynamic SQL

When `sp_describe_first_result_set` cannot infer columns (e.g. dynamic SQL), declare them manually in `Overrides`:

```jsonc
"Overrides": [
  {
    "Procedure": "dbo.SP_DynamicReport",
    "ResultColumns": ["UserId:int", "DisplayName:string", "Amount:decimal?"]
  }
]
```

## License

See [LICENSE](LICENSE).
