# SpToolKit

SpToolKit generates strongly-typed C# wrappers around SQL Server stored procedures and provides a small runtime to execute them. You maintain a JSON configuration, run a CLI against your database to emit `.g.cs` files, and inject `IStoredProcedureExecutor` where generated classes call into the database. The abstractions stay separate from the ADO.NET/EF Core implementation so you can keep generated code stable while swapping execution details.

## Requirements

- **.NET 10** (target framework for this repository’s projects)
- **SQL Server** (metadata and execution are oriented toward T-SQL stored procedures)

## Installation

**NuGet**  
Published package IDs (see [docs/PUBLISHING.md](docs/PUBLISHING.md) for versioning, CI, and first-time publish):

| Package | Purpose |
|---------|---------|
| `SpToolkit.Abstractions` | Contracts and models used by generated code |
| `SpToolkit.Runtime` | `IStoredProcedureExecutor`, `StoredProcedureExecutor`, `AddSpToolkit` |
| `SpToolkit.Generator.Cli` | .NET **global tool**; command: `sp-generate` |

**Current published version (stable): `0.1.0`**

```bash
dotnet add package SpToolkit.Abstractions --version 0.1.0
dotnet add package SpToolkit.Runtime --version 0.1.0
dotnet tool install --global SpToolkit.Generator.Cli --version 0.1.0
```

`dotnet add package` resolves stable versions automatically. For prerelease versions, add `--prerelease` or pass the explicit `--version` string.

**Project reference**  
Add references to the library projects you need, for example:

```xml
<ItemGroup>
  <ProjectReference Include="..\SpToolKit\src\SpToolkit.Abstractions\SpToolkit.Abstractions.csproj" />
  <ProjectReference Include="..\SpToolKit\src\SpToolkit.Runtime\SpToolkit.Runtime.csproj" />
</ItemGroup>
```

Adjust the paths to match where you cloned or vendored the repository. Code generation is `SpToolkit.Generator.Cli`; run it from source with `dotnet run` (see Quick Start) or install the `SpToolkit.Generator.Cli` global tool from NuGet or a local pack.

## Documentation

- [CHANGELOG.md](CHANGELOG.md) — release notes
- [docs/PUBLISHING.md](docs/PUBLISHING.md) — branches, tags, packing, GitHub Actions, NuGet, releases

## Release discipline (summary)

1. Update **CHANGELOG** (move items from `[Unreleased]` into the new version section).
2. Bump **`Version`** in [Directory.Build.props](Directory.Build.props) (or rely on CI `-p:Version=…` from the tag).
3. Push **`main`** and confirm the **CI** workflow is green.
4. Create an annotated **Git tag** that matches the package version, e.g. `v0.1.0` or `v1.0.0`:  
   `git tag -a v0.1.0 -m "v0.1.0" && git push origin v0.1.0`
5. Open a **GitHub Release** from that tag; paste the matching **CHANGELOG** section as the release notes (see [docs/PUBLISHING.md](docs/PUBLISHING.md)).

## Quick Start

1. **Create or edit the config file**  
   Add a `sptoolkit.json` (or copy from the example below) with at least connection string, output directory, and namespace—or plan to pass those via CLI flags (`--connection`, `--output`, `--namespace`).

2. **Run the generator CLI**  
   From the repository root (or with paths adjusted), for example:

   ```bash
   dotnet run --project src/SpToolkit.Generator.Cli/SpToolkit.Generator.Cli.csproj -- --config sptoolkit.json
   ```

   Use `--dry-run` to print the resolved configuration without writing files. When the `SpToolkit.Generator.Cli` tool is installed, the entry point is `sp-generate`.

3. **Register the executor in DI**  
   In your application startup, call `AddSpToolkit` (or `AddSpToolkit<TDbContext>` when reusing an EF Core context). That registers `IStoredProcedureExecutor` for use by generated wrapper types.

   ```csharp
   services.AddSpToolkit(opts =>
   {
       opts.ConnectionString = configuration.GetConnectionString("Default");
   });
   ```

## Configuration example

A commented template with all supported options is in the repository:

[sptoolkit.example.jsonc](sptoolkit.example.jsonc)

Copy it to `sptoolkit.json` (or another path and pass `--config` / `-c`), then fill in values for your environment.

## License
See [LICENSE](LICENSE).
