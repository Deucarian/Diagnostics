using System;
using System.Collections.Generic;
using System.Linq;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.Diagnostics.Editor
{
    [InitializeOnLoad]
    internal static class DiagnosticsControlCenterIntegration
    {
        private const string PackageId = "com.deucarian.diagnostics";

        static DiagnosticsControlCenterIntegration()
        {
            DeucarianToolRegistry.Register(new DeucarianToolDescriptor(
                DeucarianToolIds.Diagnostics,
                "Diagnostics",
                "Inspect local diagnostic providers and export a snapshot.",
                DeucarianControlCenterArea.Diagnostics,
                DiagnosticsWindow.OpenWindow,
                PackageId,
                "console.infoicon",
                new[] { "health", "snapshot", "providers", "logging" },
                10, createPage: DiagnosticsWindow.CreatePage));
            DeucarianControlCenterRegistry.RegisterCardProvider(
                new DiagnosticsCardProvider());
        }
    }

    internal readonly struct DiagnosticsControlCenterSnapshot
    {
        public DiagnosticsControlCenterSnapshot(
            bool hasReport,
            DiagnosticSeverity severity,
            int sectionCount,
            int itemCount,
            int warningCount,
            int errorCount,
            DateTime generatedAtUtc)
        {
            HasReport = hasReport;
            Severity = severity;
            SectionCount = Math.Max(0, sectionCount);
            ItemCount = Math.Max(0, itemCount);
            WarningCount = Math.Max(0, warningCount);
            ErrorCount = Math.Max(0, errorCount);
            GeneratedAtUtc = generatedAtUtc;
        }

        public bool HasReport { get; }
        public DiagnosticSeverity Severity { get; }
        public int SectionCount { get; }
        public int ItemCount { get; }
        public int WarningCount { get; }
        public int ErrorCount { get; }
        public DateTime GeneratedAtUtc { get; }
    }

    internal static class DiagnosticsControlCenterStatus
    {
        private static DiagnosticsControlCenterSnapshot cached;

        public static DiagnosticsControlCenterSnapshot Capture()
        {
            return cached;
        }

        public static void Refresh()
        {
            Publish(DiagnosticProviderRegistry.BuildReport());
        }

        internal static void Publish(DiagnosticReport report)
        {
            if (report == null)
            {
                cached = default;
                return;
            }

            DiagnosticItem[] items = (report.Sections ?? new List<DiagnosticSection>())
                .Where(section => section != null)
                .SelectMany(section => section.Items ?? new List<DiagnosticItem>())
                .Where(item => item != null)
                .ToArray();
            cached = new DiagnosticsControlCenterSnapshot(
                true,
                report.Severity,
                report.Sections != null ? report.Sections.Count : 0,
                items.Length,
                items.Count(item => item.Severity == DiagnosticSeverity.Warning),
                items.Count(item => item.Severity == DiagnosticSeverity.Error),
                report.GeneratedAtUtc);
        }
    }

    internal sealed class DiagnosticsCardProvider :
        IDeucarianControlCenterCardProvider
    {
        public string Id => "com.deucarian.diagnostics.status";

        public IEnumerable<DeucarianControlCenterCard> Capture(
            DeucarianControlCenterContext context)
        {
            yield return CreateCard(DiagnosticsControlCenterStatus.Capture());
        }

        internal static DeucarianControlCenterCard CreateCard(
            DiagnosticsControlCenterSnapshot snapshot)
        {
            string generated = snapshot.HasReport
                ? snapshot.GeneratedAtUtc.ToUniversalTime().ToString("u")
                : "Not generated";
            return new DeucarianControlCenterCard(
                "diagnostics.summary",
                DeucarianControlCenterArea.Diagnostics,
                "Diagnostics",
                "Sanitized aggregate health from explicitly registered providers.",
                "com.deucarian.diagnostics",
                snapshot.HasReport
                    ? ToControlCenterStatus(snapshot.Severity)
                    : DeucarianControlCenterStatus.Info,
                snapshot.HasReport ? GetStatusText(snapshot.Severity) : "Refresh to generate",
                10,
                new[]
                {
                    snapshot.SectionCount + " provider section(s), " +
                    snapshot.ItemCount + " check(s).",
                    snapshot.WarningCount + " warning(s), " +
                    snapshot.ErrorCount + " error(s).",
                    "Generated: " + generated + "."
                },
                new[]
                {
                    new DeucarianControlCenterAction(
                        "diagnostics.open",
                        "Open Diagnostics",
                        DiagnosticsWindow.OpenWindow,
                        "Inspect the full local diagnostic report."),
                    new DeucarianControlCenterAction(
                        "diagnostics.refresh-summary",
                        "Refresh Summary",
                        DiagnosticsControlCenterStatus.Refresh,
                        "Explicitly run registered providers and update this aggregate.")
                },
                new[] { "diagnostics", "health", "errors", "warnings", "snapshot" });
        }

        private static DeucarianControlCenterStatus ToControlCenterStatus(
            DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Success:
                    return DeucarianControlCenterStatus.Success;
                case DiagnosticSeverity.Warning:
                    return DeucarianControlCenterStatus.Warning;
                case DiagnosticSeverity.Error:
                    return DeucarianControlCenterStatus.Error;
                default:
                    return DeucarianControlCenterStatus.Info;
            }
        }

        private static string GetStatusText(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Success: return "Healthy";
                case DiagnosticSeverity.Warning: return "Warnings reported";
                case DiagnosticSeverity.Error: return "Errors reported";
                default: return "Informational";
            }
        }
    }
}
