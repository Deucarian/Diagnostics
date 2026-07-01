# Deucarian Diagnostics

## What this is

`com.deucarian.diagnostics` is a local diagnostics package for Unity developers and runtime debug builds.

It provides:

- a small diagnostics report model,
- explicit provider registration,
- deterministic report building,
- JSON export/copy support through Newtonsoft.Json,
- an optional runtime overlay that can be manually added or toggled from the editor,
- an editor Diagnostics Window under **Tools > Deucarian > Diagnostics > Diagnostics Window**,
- an explicit Deucarian Logging integration helper.

This package is not analytics, telemetry, remote logging, crash reporting, or an upload platform. It does not send data anywhere.

## When to use it

- You need explicit local diagnostic snapshots from runtime systems.
- You want a development/runtime overlay that can copy report JSON.
- You need an editor diagnostics view under **Tools > Deucarian > Diagnostics > Diagnostics Window**.
- You want recent Deucarian Logging entries to appear in local diagnostic reports after opt-in.

## When not to use it

- Do not use this as a telemetry, analytics, crash reporting, or upload service.
- Do not use it to own Logging behavior; Logging remains owned by `com.deucarian.logging`.
- Do not use it as a generic editor shell; shared editor chrome remains owned by `com.deucarian.editor`.

## Install

Install through the Deucarian Package Installer or Unity Package Manager.

Stable:

```json
"com.deucarian.diagnostics": "https://github.com/Deucarian/Diagnostics.git#main"
```

Development:

```json
"com.deucarian.diagnostics": "https://github.com/Deucarian/Diagnostics.git#develop"
```

Current package version: `0.1.2`.

Dependencies:

- `com.deucarian.editor` for editor-only window chrome.
- `com.deucarian.logging` for package-owned diagnostics categories and optional recent-log capture.
- `com.unity.nuget.newtonsoft-json` for report export.

## Unity compatibility

Requires Unity 2021.3 or newer.

## 60-second quick start

Install the package, register a provider, then build and export a report:

```csharp
using System;
using Deucarian.Diagnostics;

IDiagnosticProvider provider = new MyDiagnosticProvider();
IDisposable registration = DiagnosticProviderRegistry.Register(provider);

DiagnosticReport report = DiagnosticProviderRegistry.BuildReport();
string json = DiagnosticsJsonExporter.ToJson(report);

registration.Dispose();
```

Open the editor view from:

```text
Tools > Deucarian > Diagnostics > Diagnostics Window
```

## Samples

Import **Diagnostics Demo** from Package Manager. It includes:

- an example provider,
- a bootstrap component that registers/unregisters the provider,
- optional runtime overlay setup,
- README notes for JSON export.

## Public API map

- `IDiagnosticProvider`: interface implemented by systems that contribute sections to a report.
- `DiagnosticProviderRegistry`: explicit provider registration, clearing, and report building.
- `DiagnosticReport`, `DiagnosticSection`, and `DiagnosticItem`: immutable report data returned by the registry.
- `DiagnosticSeverity` and `DiagnosticSeverityUtility`: severity values and helpers.
- `DiagnosticsJsonExporter`: Newtonsoft.Json export for reports.
- `DiagnosticsLog`: package-owned logging categories.
- `DeucarianLoggingDiagnosticsInstaller`: opt-in bridge that registers recent Deucarian Logging entries.
- `RuntimeDiagnosticsOverlay`: optional runtime view that displays and copies snapshots.

## Integrations

Works with:

- `com.deucarian.logging` for opt-in recent-log capture.
- `com.deucarian.editor` for the Diagnostics editor window shell.
- Unity's Newtonsoft Json package for JSON export.

Optional integrations:

- Object Loading can register its own diagnostics provider when both packages are installed. Object Loading does not depend on Diagnostics.

Does not own:

- telemetry or remote upload,
- Logging package ownership,
- Package Installer behavior,
- generic editor shell resources.

## Provider registration

Diagnostics providers are registered explicitly:

```csharp
using Deucarian.Diagnostics;

IDiagnosticProvider provider = new MyDiagnosticProvider();
IDisposable registration = DiagnosticProviderRegistry.Register(provider);

DiagnosticReport report = DiagnosticProviderRegistry.BuildReport();
string json = DiagnosticsJsonExporter.ToJson(report);

registration.Dispose();
```

Provider exceptions are captured as diagnostic error sections so one failing provider does not prevent the rest of the snapshot from being built.

## Logging integration

Diagnostics can expose recent Deucarian Logging entries when you opt in to a ring buffer sink:

```csharp
using Deucarian.Diagnostics;

DeucarianLoggingDiagnosticsInstaller installer =
    DeucarianLoggingDiagnosticsInstaller.Install();
```

Dispose the installer when the integration should be removed:

```csharp
installer.Dispose();
```

This does not emit startup logs and does not auto-register.

## Runtime overlay

Enable **Show Runtime Overlay** in the Diagnostics Window when you want a simple in-game debug view in the active scene. The toggle reuses an existing `RuntimeDiagnosticsOverlay` when one is present. If none exists, it instantiates the package-owned overlay prefab when available, or creates a GameObject with `RuntimeDiagnosticsOverlay` as a fallback.

Disable **Show Runtime Overlay** to hide the existing overlay without deleting it. Opening the Diagnostics Window is passive and does not create, enable, disable, or remove scene objects.

You can also add `RuntimeDiagnosticsOverlay` to a GameObject manually. The overlay builds reports from the current registry and can copy the current report JSON to the system clipboard.

The overlay is intended for Editor and development/debug builds. It does not create itself, open windows, or register providers automatically.

## Editor window

Open **Tools > Deucarian > Diagnostics > Diagnostics Window**.

The window refreshes only when opened or when you press **Refresh**. It can copy the current report as JSON and includes a **Show Runtime Overlay** toggle for the active scene.

## Object Loading integration

Object Loading does not require Diagnostics.

Object Loading diagnostics integration is optional. Object Loading does not depend on Diagnostics, but it includes an optional diagnostics assembly that compiles only when `com.deucarian.diagnostics` is installed.

Register the Object Loading diagnostics provider explicitly from the code that owns the `ObjectLoadingPipeline` when you want Object Loading state, latest load results, errors, timings, and loaded object metadata inside diagnostic snapshots.

## Troubleshooting

- If a report is empty, confirm at least one `IDiagnosticProvider` is registered before calling `BuildReport()`.
- If one provider fails, check the generated diagnostic error section; the rest of the snapshot should still build.
- If the runtime overlay does not appear, open the Diagnostics Window and enable **Show Runtime Overlay**, or add `RuntimeDiagnosticsOverlay` to a GameObject manually.
- If JSON export fails to compile, confirm `com.unity.nuget.newtonsoft-json` is installed at the version declared in `package.json`.

## Validation

Run the shared package validator from the repository root:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Run the package's EditMode tests in Unity after code or assembly definition changes. Tests cover provider registration, deterministic clearing, provider exception isolation, JSON export, severity aggregation, and logging provider behavior.

Documentation-only updates should still pass:

```powershell
git diff --check
```

## Architecture / Contributor Notes

- [AGENTS.md](AGENTS.md) contains repository-specific ownership and Codex guidance.
- Deucarian architecture rules live in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/develop/ARCHITECTURE.md).
- Capability ownership is tracked in [CAPABILITY_OWNERSHIP.md](https://github.com/Deucarian/Package-Registry/blob/develop/CAPABILITY_OWNERSHIP.md).

## License

See [LICENSE.md](LICENSE.md).
