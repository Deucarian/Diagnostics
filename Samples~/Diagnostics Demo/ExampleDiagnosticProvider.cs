using Deucarian.Diagnostics;
using UnityEngine;

namespace Deucarian.Diagnostics.Samples.DiagnosticsDemo
{
    public sealed class ExampleDiagnosticProvider : IDiagnosticProvider
    {
        public string ProviderId
        {
            get { return "sample.diagnostics"; }
        }

        public string DisplayName
        {
            get { return "Diagnostics Demo"; }
        }

        public void Collect(DiagnosticReportBuilder builder)
        {
            builder.AddSection(ProviderId, DisplayName)
                .AddItem("unity_version", "Unity Version", Application.unityVersion, DiagnosticSeverity.Info)
                .AddItem("platform", "Platform", Application.platform.ToString(), DiagnosticSeverity.Info)
                .AddItem("target_frame_rate", "Target Frame Rate", Application.targetFrameRate.ToString(), DiagnosticSeverity.Info);
        }
    }
}
