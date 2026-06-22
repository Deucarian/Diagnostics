using System.Collections.Generic;
using Deucarian.Logging;
using NUnit.Framework;

namespace Deucarian.Diagnostics.Tests
{
    public sealed class DiagnosticsLogTests
    {
        private CapturingSink sink;

        [SetUp]
        public void SetUp()
        {
            DeucarianLogSettings.ResetToDefaults();
            DeucarianLog.ClearSinks();
            sink = new CapturingSink();
            DeucarianLog.RegisterSink(sink);
        }

        [TearDown]
        public void TearDown()
        {
            DeucarianLogSettings.ResetToDefaults();
            DeucarianLog.ResetSinksToDefault();
        }

        [Test]
        public void EditorWarningUsesDiagnosticsEditorCategory()
        {
            DiagnosticsLog.Editor.Warning("Cannot create RuntimeDiagnosticsOverlay because there is no valid active scene.");

            Assert.AreEqual(1, sink.Entries.Count);
            Assert.AreEqual(DeucarianLogLevel.Warning, sink.Entries[0].Level);
            Assert.AreEqual("Diagnostics.Editor", sink.Entries[0].Category);
            Assert.AreEqual("Cannot create RuntimeDiagnosticsOverlay because there is no valid active scene.", sink.Entries[0].Message);
        }

        private sealed class CapturingSink : IDeucarianLogSink
        {
            private readonly List<DeucarianLogEntry> entries = new List<DeucarianLogEntry>();

            public IReadOnlyList<DeucarianLogEntry> Entries
            {
                get { return entries; }
            }

            public void Log(in DeucarianLogEntry entry)
            {
                entries.Add(entry);
            }
        }
    }
}
