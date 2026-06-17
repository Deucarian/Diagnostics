using System;
using NUnit.Framework;

namespace Deucarian.Diagnostics.Tests
{
    public sealed class DiagnosticProviderRegistryTests
    {
        [SetUp]
        public void SetUp()
        {
            DiagnosticProviderRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            DiagnosticProviderRegistry.Clear();
        }

        [Test]
        public void RegisterBuildAndDisposeAreDeterministic()
        {
            CountingProvider provider = new CountingProvider();

            IDisposable registration = DiagnosticProviderRegistry.Register(provider);
            DiagnosticReport report = DiagnosticProviderRegistry.BuildReport();

            Assert.AreEqual(1, provider.CollectCount);
            Assert.AreEqual(1, report.Sections.Count);

            registration.Dispose();
            report = DiagnosticProviderRegistry.BuildReport();

            Assert.AreEqual(0, report.Sections.Count);
        }

        [Test]
        public void ProviderExceptionBecomesDiagnosticErrorItem()
        {
            DiagnosticProviderRegistry.Register(new ThrowingProvider());

            DiagnosticReport report = DiagnosticProviderRegistry.BuildReport();

            Assert.AreEqual(DiagnosticSeverity.Error, report.Severity);
            Assert.AreEqual(1, report.Sections.Count);
            Assert.AreEqual("provider_exception", report.Sections[0].Items[0].Key);
        }

        [Test]
        public void SeverityAggregationUsesHighestSeverity()
        {
            DiagnosticReportBuilder builder = new DiagnosticReportBuilder();
            builder.AddSection("demo", "Demo")
                .AddItem("ok", "OK", "yes", DiagnosticSeverity.Success)
                .AddItem("warning", "Warning", "careful", DiagnosticSeverity.Warning);

            DiagnosticReport report = builder.Build();

            Assert.AreEqual(DiagnosticSeverity.Warning, report.Severity);
            Assert.AreEqual(DiagnosticSeverity.Warning, report.Sections[0].Severity);
        }

        private sealed class CountingProvider : IDiagnosticProvider
        {
            public int CollectCount { get; private set; }

            public string ProviderId
            {
                get { return "counting"; }
            }

            public string DisplayName
            {
                get { return "Counting"; }
            }

            public void Collect(DiagnosticReportBuilder builder)
            {
                CollectCount++;
                builder.AddSection(ProviderId, DisplayName)
                    .AddItem("count", "Count", CollectCount.ToString(), DiagnosticSeverity.Success);
            }
        }

        private sealed class ThrowingProvider : IDiagnosticProvider
        {
            public string ProviderId
            {
                get { return "throwing"; }
            }

            public string DisplayName
            {
                get { return "Throwing"; }
            }

            public void Collect(DiagnosticReportBuilder builder)
            {
                throw new InvalidOperationException("Provider failed.");
            }
        }
    }
}
