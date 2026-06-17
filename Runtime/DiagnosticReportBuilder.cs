using System;
using System.Collections.Generic;

namespace Deucarian.Diagnostics
{
    public sealed class DiagnosticReportBuilder
    {
        private readonly DiagnosticReport report;

        public DiagnosticReportBuilder()
        {
            report = new DiagnosticReport();
        }

        public DiagnosticSection AddSection(string id, string title)
        {
            DiagnosticSection section = new DiagnosticSection(
                NormalizeId(id),
                string.IsNullOrWhiteSpace(title) ? NormalizeId(id) : title.Trim());
            report.Sections.Add(section);
            return section;
        }

        public DiagnosticReport Build()
        {
            return report;
        }

        public static DiagnosticReport BuildFrom(IEnumerable<IDiagnosticProvider> providers)
        {
            DiagnosticReportBuilder builder = new DiagnosticReportBuilder();
            if (providers == null)
            {
                return builder.Build();
            }

            foreach (IDiagnosticProvider provider in providers)
            {
                if (provider == null)
                {
                    continue;
                }

                try
                {
                    provider.Collect(builder);
                }
                catch (Exception exception)
                {
                    AddProviderFailure(builder, provider, exception);
                }
            }

            return builder.Build();
        }

        private static void AddProviderFailure(DiagnosticReportBuilder builder,
                                               IDiagnosticProvider provider,
                                               Exception exception)
        {
            string providerId = NormalizeId(provider.ProviderId);
            string title = string.IsNullOrWhiteSpace(provider.DisplayName)
                ? providerId
                : provider.DisplayName.Trim();

            builder.AddSection(providerId + ".failure", title)
                .AddItem(
                    "provider_exception",
                    "Provider Exception",
                    exception != null ? exception.GetType().Name : "Exception",
                    DiagnosticSeverity.Error,
                    exception != null ? exception.Message : "Diagnostic provider failed.");
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? "diagnostics" : id.Trim();
        }
    }
}
