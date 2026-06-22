# Changelog

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
