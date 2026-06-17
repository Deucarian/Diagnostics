using System.Collections.Generic;

namespace Deucarian.Diagnostics
{
    public static class DiagnosticSeverityUtility
    {
        public static DiagnosticSeverity Aggregate(IEnumerable<DiagnosticItem> items)
        {
            DiagnosticSeverity severity = DiagnosticSeverity.Success;
            if (items == null)
            {
                return severity;
            }

            foreach (DiagnosticItem item in items)
            {
                if (item != null)
                {
                    severity = Max(severity, item.Severity);
                }
            }

            return severity;
        }

        public static DiagnosticSeverity Aggregate(IEnumerable<DiagnosticSection> sections)
        {
            DiagnosticSeverity severity = DiagnosticSeverity.Success;
            if (sections == null)
            {
                return severity;
            }

            foreach (DiagnosticSection section in sections)
            {
                if (section != null)
                {
                    severity = Max(severity, section.Severity);
                }
            }

            return severity;
        }

        public static DiagnosticSeverity Max(DiagnosticSeverity left, DiagnosticSeverity right)
        {
            return right > left ? right : left;
        }
    }
}
