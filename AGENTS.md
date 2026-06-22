# Deucarian Diagnostics Agent Notes

Package ID: `com.deucarian.diagnostics`
Repository: `Deucarian/Diagnostics`

Follow the canonical Deucarian governance docs in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/develop/ARCHITECTURE.md), especially capability ownership and dependency rules.

## Ownership

This package owns:

- Local diagnostics snapshots, providers, JSON export, overlays, aggregation, and diagnostics views.

Registered capabilities:
- `diagnostics`

This package must not own:

- Telemetry/uploading, Logging ownership, package installation, or unrelated editor shell ownership.

## Dependencies

Allowed dependency shape:

- May depend on Logging, Editor for editor views, and Newtonsoft for JSON export.

Required dependencies and why:

- `com.deucarian.editor`: shared editor shell/resources.
- `com.deucarian.logging`: package logging facade and diagnostics output.
- `com.unity.nuget.newtonsoft-json`: JSON serialization package used by this package.

Optional/version-defined dependencies:

- None.

Architecture exceptions:

- None.

## Policies

- Logging: Use Logging; no direct Unity Debug calls.
- Common: Do not add Common unless diagnostics production code directly owns transient Unity cleanup.
- Editor UI: Use Editor shell for editor views; do not own shared editor resources.
- Diagnostics: This package owns local diagnostics only; no telemetry/upload pipelines.
- Testing: Tests should verify snapshots/export/providers without introducing upload behavior.

## Validation

Run the shared validator before committing:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Also run existing repository tests when changing code or asmdefs. Documentation-only updates should still run `git diff --check`.

## Codex Guidance

- Inspect current files before changing anything.
- Work on `develop`; do not edit or merge `main` unless the task is promotion-only.
- Do not edit `Library/PackageCache`.
- Do not guess package versions or dependency versions.
- Do not add package dependencies casually; update asmdefs, `package.json`, `deucarian-package.json`, Package Registry, and fallback catalogs together when a dependency is truly required.
- Do not create local copies of shared helpers.
- Keep commits focused and report exactly what changed and what was validated.

## Before Adding Code

- Confirm the change fits this package's ownership boundary.
- Reuse existing local patterns and helpers.
- Avoid broad refactors without audit support.
- Preserve runtime/editor behavior unless the task explicitly asks to change it.

## Before Adding A Dependency

- Is the capability already owned by that package?
- Is it used by production code, editor code, sample code, or tests?
- Does the asmdef reference match `package.json`?
- Does `deucarian-package.json` need updating?
- Does Package Registry need updating?
- Does Package Installer fallback catalog need updating?
- Does Bootstrap fallback catalog need updating?
- Are exact versions propagated without guessing?

## Before Adding A Helper

- Is this package the capability owner?
- Is this behavior repeated in at least three production packages?
- Is there an existing owner package?
- Should this remain local?
- Has the audit been updated?

## Debug And Unity Object Lifetime

- Use Deucarian Logging for package diagnostics; direct Unity Debug calls are forbidden.
- Do not copy Common lifetime helpers. Add Common only if production code directly owns transient Unity object cleanup.
- Test fixture teardown may use `DestroyImmediate` directly.
