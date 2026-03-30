# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- NuGet packaging metadata, symbols, and publish workflow (in progress).

### Changed

- README, samples, and configuration examples as the project moves toward a stable public install story.

### Breaking

- None planned; any incompatible change will be listed here before the next tagged release.

## [0.1.0] - 2026-03-29

### Added

- CLI generator for strongly-typed SQL Server stored procedure wrappers (JSON-driven configuration, schema filtering, dry-run).
- Runtime package with `IStoredProcedureExecutor` and ADO.NET / optional EF Core–integrated execution.
- JSON configuration for connection string, output paths, namespaces, naming overrides, and generator options.
- Dependency injection extensions (`AddSpToolkit`, `AddSpToolkit<TDbContext>`) registering `IStoredProcedureExecutor`.

### Breaking

- `IStoredProcedureExecutor` includes `ExecuteAsync<TInput>(string procedureName, TInput input, CancellationToken cancellationToken = default)`; custom implementations must provide this member.
