# Changelog

## 0.1.3 - 2026-07-15

- Migrated the Diagnostics Window to the shared headerless Deucarian workbench with canonical 900/1180 responsive modes, toolbar actions, shared panels, and shared status footer.
- Preserved snapshot refresh, JSON copy, runtime-overlay toggling, and the existing Play-safe scene guards, with structural and interaction coverage.

## 0.1.2 - 2026-06-22

- Updated exact `com.deucarian.editor` and `com.deucarian.logging` dependencies for the accepted stable release line.

## 0.1.1 - 2026-06-22

- Added package-owned Diagnostics log categories backed by Deucarian Logging.
- Routed the editor RuntimeDiagnosticsOverlay warning through `Diagnostics.Editor` instead of direct Unity Debug logging.

## 0.1.0

- Added the initial local diagnostics model.
- Added explicit provider registration and report building.
- Added provider exception isolation.
- Added Newtonsoft.Json report export and clipboard copy support.
- Added explicit Deucarian Logging ring buffer diagnostics integration.
- Added a manually attached runtime diagnostics overlay.
- Added a Deucarian editor diagnostics window.
- Added the Diagnostics Demo sample.
- Added EditMode tests.
