# Publishing and releases

This document describes versioning, Git branching, CI/CD, NuGet, and GitHub Releases for SpToolKit. It complements the root [README](../README.md) and [CHANGELOG](../CHANGELOG.md).

## Semantic versioning

- Packages follow **SemVer 2** versions: stable `MAJOR.MINOR.PATCH` or prerelease `MAJOR.MINOR.PATCH-<label>` (for example `0.1.0-preview.1`).
- The default version in [Directory.Build.props](../Directory.Build.props) should be updated when you cut a release (or rely on CI to pass `-p:Version=…` from the Git tag).
- **Git tags** for releases use a leading `v` and match the package version without it: tag `v1.2.3` → package `1.2.3`; tag `v0.1.0-preview.1` → package `0.1.0-preview.1`.
- **Build metadata** (`+…` after the version) is not supported by the release workflow; omit it for tags and manual runs.
- On NuGet, prerelease packages sort below stable versions for the same `MAJOR.MINOR.PATCH`; consumers must opt in to prerelease (for example `--prerelease` with the CLI).

## Branch and pull request policy

- **`main`**: integration branch; keep it in a releasable state. CI runs on pushes and pull requests targeting `main` (see `.github/workflows/ci.yml`).
- **`develop`**: optional; use only if your team wants a long-lived integration branch. This repository does not require it.
- **Pull requests**: use PRs into `main` with review before merge when more than one person contributes.

Enforcement (required reviewers, required status checks) is configured in **GitHub → Settings → Branches** and is not represented as files in this repo.

## Centralized versioning

[Directory.Build.props](../Directory.Build.props) at the repository root sets:

- `Version`, `AssemblyVersion`, `FileVersion`, `InformationalVersion` (defaults aligned for `0.1.0`).

Packable projects inherit these values unless overridden. Release automation passes `-p:Version=…` so the **tag** drives the shipped package version. `AssemblyVersion` and `FileVersion` use the numeric core of `Version` (the part before `-`), with a `.0` revision—prerelease labels apply to the NuGet package version and informational metadata, not to a four-part assembly version string.

## Packable packages

| Package ID | Project | Role |
|------------|---------|------|
| `SpToolkit.Abstractions` | `src/SpToolkit.Abstractions` | Contracts and models |
| `SpToolkit.Runtime` | `src/SpToolkit.Runtime` | Executor and DI extensions |
| `SpToolkit.Generator.Cli` | `src/SpToolkit.Generator.Cli` | **.NET tool**; command name **`sp-generate`** |

`SpToolkit.Generator` (engine) is **not** packable; it is referenced by the CLI only.

## Local pack and tool smoke test

From the repository root, after a Release build:

```bash
dotnet build SpToolkit.slnx -c Release
dotnet pack SpToolkit.slnx -c Release -o ./artifacts --no-build
```

Install the tool from a local folder (example paths for your clone):

```bash
dotnet tool install --global SpToolkit.Generator.Cli --version 0.1.0 --add-source ./artifacts
sp-generate --help
dotnet tool uninstall --global SpToolkit.Generator.Cli
```

Adjust `--version` to match the package version you packed.

## Continuous integration

- **CI** (`.github/workflows/ci.yml`): on push/PR to `main`, runs `dotnet restore`, `dotnet build -c Release`, and `dotnet test` on `SpToolkit.slnx`.
- **Release** (`.github/workflows/release.yml`):
  - Runs when a tag matching `v*.*.*` or `v*.*.*-*` is pushed, or when **Run workflow** is used (workflow dispatch).
  - Validates versions as `vX.Y.Z` or `vX.Y.Z-<prerelease>` (tag push), and `X.Y.Z` or `X.Y.Z-<prerelease>` (workflow dispatch). No leading zeros in numeric parts; prerelease labels follow SemVer 2 (dot-separated identifiers). Build metadata (`+…`) is rejected.
  - Builds and tests in Release, packs the three packages, uploads **artifacts** (`.nupkg` and `.snupkg`).
  - **NuGet push**: configure the repository secret `NUGET_API_KEY`. On **tag** builds, if the secret is set, packages are pushed to nuget.org automatically; if unset, the job still succeeds and you push manually. For **workflow dispatch**, check **publish_nuget** to push (secret required).

Do not commit API keys. Use **GitHub → Settings → Secrets and variables → Actions**.

## GitHub Releases

Creating a **GitHub Release** is not fully automated in this repository. Recommended manual flow:

1. Ensure [CHANGELOG.md](../CHANGELOG.md) reflects the version (move items from `[Unreleased]` if needed).
2. Commit any version bumps in `Directory.Build.props` if you want the default in-repo version to match (optional when tags drive CI).
3. Create and push an annotated tag (stable or prerelease), for example `git tag -a v1.0.0 -m "v1.0.0"` then `git push origin v1.0.0`, or `git tag -a v0.1.0-preview.1 -m "v0.1.0-preview.1"` then `git push origin v0.1.0-preview.1`.
4. Wait for the **Release** workflow; download artifacts or confirm NuGet push.
5. On GitHub: **Releases → Draft a new release**, choose the tag, title `v1.0.0`, paste the changelog section for that version as the description, publish.

## First publish to nuget.org

1. Create a nuget.org account and API key scoped to push the desired packages (or organization).
2. Reserve or confirm package IDs: `SpToolkit.Abstractions`, `SpToolkit.Runtime`, `SpToolkit.Generator.Cli`.
3. Add `NUGET_API_KEY` to GitHub Actions secrets (or push from a trusted machine):

   ```bash
   dotnet nuget push ./artifacts/*.nupkg --api-key <KEY> --source https://api.nuget.org/v3/index.json
   dotnet nuget push ./artifacts/*.snupkg --api-key <KEY> --source https://api.nuget.org/v3/index.json
   ```

4. Verify listing pages: README, license (MIT), dependencies (especially `SpToolkit.Runtime`), and tool command `sp-generate`.

## Version pinning in consuming projects

- Prefer **fixed** `PackageReference` versions (e.g. `Version="0.1.0"`) in libraries you publish.
- Avoid floating versions (`*`, `10.*`) in **your own** shipped packages’ dependencies; consumers can still choose floating in apps if they accept the risk.

## Deprecation policy (optional)

If you must rename a package ID or obsolete an API:

1. Use **NuGet deprecation** on the old package ID pointing to the replacement.
2. Document the migration in [CHANGELOG.md](../CHANGELOG.md) and the README.

## SourceLink and symbols

Packable projects reference **Microsoft.SourceLink.GitHub** and emit **`.snupkg`** symbol packages when you run `dotnet pack`. Push `.snupkg` files alongside `.nupkg` so consumers can debug into published assemblies.

## Dependabot

[.github/dependabot.yml](../.github/dependabot.yml) opens PRs for NuGet and GitHub Actions updates on a schedule. Merge or adjust as needed.

## Authors metadata

`Authors` in `Directory.Build.props` is set to match the root **LICENSE** copyright holder. Override when packing if needed: `dotnet pack -p:Authors="Your Name"`.
