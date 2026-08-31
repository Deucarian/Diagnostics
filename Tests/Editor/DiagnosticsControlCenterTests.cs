using System;
using System.Linq;
using Deucarian.Diagnostics.Editor;
using Deucarian.Editor;
using NUnit.Framework;

namespace Deucarian.Diagnostics.Tests
{
    public sealed class DiagnosticsControlCenterTests
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
        public void AutomaticCardCaptureDoesNotCollectProviders()
        {
            CountingProvider provider = new CountingProvider();
            DiagnosticProviderRegistry.Register(provider);

            DiagnosticsCardProvider cardProvider = new DiagnosticsCardProvider();
            DeucarianControlCenterCard card = cardProvider.Capture(
                new DeucarianControlCenterContext(DateTime.UtcNow, false)).Single();

            Assert.AreEqual(0, provider.CollectCount);
            Assert.AreEqual("diagnostics.summary", card.Id);
        }

        [Test]
        public void CardContainsOnlySanitizedAggregateAndExplicitRefresh()
        {
            DiagnosticsControlCenterSnapshot snapshot =
                new DiagnosticsControlCenterSnapshot(
                    true, DiagnosticSeverity.Error, 2, 7, 1, 2,
                    new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc));

            DeucarianControlCenterCard card =
                DiagnosticsCardProvider.CreateCard(snapshot);

            Assert.AreEqual(DeucarianControlCenterStatus.Error, card.Status);
            Assert.That(card.Details, Has.Some.Contains("2 provider section(s), 7 check(s)"));
            Assert.AreEqual("diagnostics.refresh-summary", card.Actions[1].Id);
            StringAssert.DoesNotContain("token", string.Join(" ", card.Details).ToLowerInvariant());
        }

        private sealed class CountingProvider : IDiagnosticProvider
        {
            public int CollectCount { get; private set; }
            public string ProviderId => "control-center-counting";
            public string DisplayName => "Control Center Counting";

            public void Collect(DiagnosticReportBuilder builder)
            {
                CollectCount++;
                builder.AddSection(ProviderId, DisplayName);
            }
        }
    }
}