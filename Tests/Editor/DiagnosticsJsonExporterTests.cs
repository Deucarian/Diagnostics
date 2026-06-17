using NUnit.Framework;

namespace Deucarian.Diagnostics.Tests
{
    public sealed class DiagnosticsJsonExporterTests
    {
        [Test]
        public void ToJsonIncludesSectionsAndItems()
        {
            DiagnosticReportBuilder builder = new DiagnosticReportBuilder();
            builder.AddSection("demo", "Demo")
                .AddItem("item", "Item", "value", DiagnosticSeverity.Success);

            string json = DiagnosticsJsonExporter.ToJson(builder.Build());

            StringAssert.Contains("\"severity\": \"Success\"", json);
            StringAssert.Contains("\"id\": \"demo\"", json);
            StringAssert.Contains("\"key\": \"item\"", json);
        }
    }
}
