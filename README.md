# Deucarian Diagnostics

## Overview

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

## Installation

Install through the Deucarian Package Installer or Unity Package Manager:

```text
https://github.com/Deucarian/Diagnostics.git#main
```

The package depends on Deucarian Editor, Deucarian Logging, and Unity's Newtonsoft Json package.

Current package version: `0.1.2`.

Deucarian Logging is a runtime dependency used for package-owned diagnostics categories and optional recent-log capture. Deucarian Editor is editor-only chrome for the Diagnostics window, and Newtonsoft Json is used for report export.

## Provider Registration

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

## Logging Integration

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

## Runtime Overlay

Enable **Show Runtime Overlay** in the Diagnostics Window when you want a simple in-game debug view in the active scene. The toggle reuses an existing `RuntimeDiagnosticsOverlay` when one is present. If none exists, it instantiates the package-owned overlay prefab when available, or creates a GameObject with `RuntimeDiagnosticsOverlay` as a fallback.

Disable **Show Runtime Overlay** to hide the existing overlay without deleting it. Opening the Diagnostics Window is passive and does not create, enable, disable, or remove scene objects.

You can also add `RuntimeDiagnosticsOverlay` to a GameObject manually. The overlay builds reports from the current registry and can copy the current report JSON to the system clipboard.

The overlay is intended for Editor and development/debug builds. It does not create itself, open windows, or register providers automatically.

## Editor Window

Open **Tools > Deucarian > Diagnostics > Diagnostics Window**.

The window refreshes only when opened or when you press **Refresh**. It can copy the current report as JSON and includes a **Show Runtime Overlay** toggle for the active scene.

## Object Loading Integration

Object Loading does not require Diagnostics.

Object Loading diagnostics integration is optional. Object Loading does not depend on Diagnostics, but it includes an optional diagnostics assembly that compiles only when `com.deucarian.diagnostics` is installed.

Register the Object Loading diagnostics provider explicitly from the code that owns the `ObjectLoadingPipeline` when you want Object Loading state, latest load results, errors, timings, and loaded object metadata inside diagnostic snapshots.

## Samples

Import **Diagnostics Demo** from Package Manager. It includes:

- an example provider,
- a bootstrap component that registers/unregisters the provider,
- optional runtime overlay setup,
- README notes for JSON export.

## Tests

Run the package's EditMode tests in Unity. Tests cover provider registration, deterministic clearing, provider exception isolation, JSON export, severity aggregation, and logging provider behavior.

## Architecture / Contributor Notes

- [AGENTS.md](AGENTS.md) contains repository-specific ownership and Codex guidance.
- Deucarian architecture rules live in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/develop/ARCHITECTURE.md).
- Capability ownership is tracked in [CAPABILITY_OWNERSHIP.md](https://github.com/Deucarian/Package-Registry/blob/develop/CAPABILITY_OWNERSHIP.md).

## License

See [LICENSE.md](LICENSE.md).
