# Diagnostics Demo

Add `DiagnosticsDemoBootstrap` to a GameObject to register the sample provider explicitly.

The sample provider contributes a section with a few local values. Enable `Add Overlay` on the bootstrap component when you also want a manually created `RuntimeDiagnosticsOverlay` in the scene.

Build a snapshot from code:

```csharp
DiagnosticReport report = DiagnosticProviderRegistry.BuildReport();
string json = DiagnosticsJsonExporter.ToJson(report);
```

The package does not send diagnostics anywhere. JSON export is local and developer controlled.
