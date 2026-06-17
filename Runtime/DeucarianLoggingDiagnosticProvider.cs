using System;
using System.Collections.Generic;
using Deucarian.Logging;

namespace Deucarian.Diagnostics
{
    public sealed class DeucarianLoggingDiagnosticProvider : IDiagnosticProvider
    {
        private readonly RingBufferLogSink ringBuffer;
        private readonly int maxRecentEntries;

        public DeucarianLoggingDiagnosticProvider(RingBufferLogSink ringBuffer, int maxRecentEntries = 20)
        {
            this.ringBuffer = ringBuffer ?? throw new ArgumentNullException(nameof(ringBuffer));
            this.maxRecentEntries = Math.Max(1, maxRecentEntries);
        }

        public string ProviderId
        {
            get { return "deucarian.logging"; }
        }

        public string DisplayName
        {
            get { return "Deucarian Logging"; }
        }

        public void Collect(DiagnosticReportBuilder builder)
        {
            IReadOnlyList<DeucarianLogEntry> entries = ringBuffer.Entries;
            int warnings = 0;
            int errors = 0;
            int exceptions = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianLogEntry entry = entries[i];
                if (entry.Level == DeucarianLogLevel.Warning)
                {
                    warnings++;
                }
                else if (entry.Level == DeucarianLogLevel.Error)
                {
                    errors++;
                }
                else if (entry.Level == DeucarianLogLevel.Exception)
                {
                    exceptions++;
                }
            }

            DiagnosticSeverity summarySeverity = errors > 0 || exceptions > 0
                ? DiagnosticSeverity.Error
                : warnings > 0
                    ? DiagnosticSeverity.Warning
                    : DiagnosticSeverity.Success;

            DiagnosticSection section = builder.AddSection(ProviderId, DisplayName);
            section.AddItem("entry_count", "Recent Entries", entries.Count.ToString(), summarySeverity);
            section.AddItem("warning_count", "Warnings", warnings.ToString(), warnings > 0 ? DiagnosticSeverity.Warning : DiagnosticSeverity.Success);
            section.AddItem("error_count", "Errors", errors.ToString(), errors > 0 ? DiagnosticSeverity.Error : DiagnosticSeverity.Success);
            section.AddItem("exception_count", "Exceptions", exceptions.ToString(), exceptions > 0 ? DiagnosticSeverity.Error : DiagnosticSeverity.Success);

            int start = Math.Max(0, entries.Count - maxRecentEntries);
            for (int i = start; i < entries.Count; i++)
            {
                DeucarianLogEntry entry = entries[i];
                section.AddItem(
                    "entry_" + (i - start + 1),
                    FormatLogLabel(entry),
                    FormatLogValue(entry),
                    ToSeverity(entry.Level),
                    entry.Exception != null ? entry.Exception.GetType().Name + ": " + entry.Exception.Message : null);
            }
        }

        private static string FormatLogLabel(DeucarianLogEntry entry)
        {
            return entry.Level + " / " + (string.IsNullOrWhiteSpace(entry.Category) ? "General" : entry.Category);
        }

        private static string FormatLogValue(DeucarianLogEntry entry)
        {
            return entry.Message ?? string.Empty;
        }

        private static DiagnosticSeverity ToSeverity(DeucarianLogLevel level)
        {
            if (level == DeucarianLogLevel.Exception || level == DeucarianLogLevel.Error)
            {
                return DiagnosticSeverity.Error;
            }

            if (level == DeucarianLogLevel.Warning)
            {
                return DiagnosticSeverity.Warning;
            }

            return DiagnosticSeverity.Info;
        }
    }
}
