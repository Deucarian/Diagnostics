using Deucarian.Logging;
using NUnit.Framework;

namespace Deucarian.Diagnostics.Tests
{
    public sealed class DeucarianLoggingDiagnosticProviderTests
    {
        [Test]
        public void CollectReportsWarningAndErrorCounts()
        {
            RingBufferLogSink sink = new RingBufferLogSink(8);
            sink.Log(new DeucarianLogEntry(System.DateTime.UtcNow, 1, DeucarianLogLevel.Info, "Demo", "Hello"));
            sink.Log(new DeucarianLogEntry(System.DateTime.UtcNow, 2, DeucarianLogLevel.Warning, "Demo", "Careful"));
            sink.Log(new DeucarianLogEntry(System.DateTime.UtcNow, 3, DeucarianLogLevel.Error, "Demo", "Broken"));

            DiagnosticReportBuilder builder = new DiagnosticReportBuilder();
            new DeucarianLoggingDiagnosticProvider(sink).Collect(builder);
            DiagnosticReport report = builder.Build();

            Assert.AreEqual(DiagnosticSeverity.Error, report.Severity);
            Assert.AreEqual("Deucarian Logging", report.Sections[0].Title);
            Assert.AreEqual("1", report.Sections[0].Items[1].Value);
            Assert.AreEqual("1", report.Sections[0].Items[2].Value);
        }
    }
}
