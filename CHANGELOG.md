# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed

### Breaking

## [0.1.0-preview.3] - 2026-03-31

### Added

### Changed

- Generator wrappers now use `EmptyRequest` as `TInput` for procedures without input parameters across `ExecuteOnly` and all `Query*` patterns, preventing compile-time mismatches when no request class is generated.

### Breaking

## [0.1.0-preview.2] - 2026-03-30

First public **prerelease** on [nuget.org](https://www.nuget.org/profiles/josuenavarro) for all packable packages at this version.

### Added

- CLI generator for strongly-typed SQL Server stored procedure wrappers (JSON-driven configuration, schema filtering, dry-run).
- Runtime package with `IStoredProcedureExecutor` and ADO.NET / optional EF Core–integrated execution.
- JSON configuration for connection string, output paths, namespaces, naming overrides, and generator options.
- Dependency injection extensions (`AddSpToolkit`, `AddSpToolkit<TDbContext>`) registering `IStoredProcedureExecutor`.
- NuGet packages: `SpToolkit.Abstractions`, `SpToolkit.Runtime`, `SpToolkit.Generator.Cli` (.NET global tool, command `sp-generate`).
- Repository packaging: centralized versioning in `Directory.Build.props`, SourceLink/snupkg for packable projects, GitHub Actions (CI + release), Dependabot, and publishing documentation.

### Breaking

- `IStoredProcedureExecutor` includes `ExecuteAsync<TInput>(string procedureName, TInput input, CancellationToken cancellationToken = default)`; custom implementations must provide this member.
