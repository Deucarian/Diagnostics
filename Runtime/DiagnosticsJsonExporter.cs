using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Deucarian.Diagnostics
{
    public static class DiagnosticsJsonExporter
    {
        public static string ToJson(DiagnosticReport report)
        {
            return ToJson(report, true);
        }

        public static string ToJson(DiagnosticReport report, bool indented)
        {
            Formatting formatting = indented ? Formatting.Indented : Formatting.None;
            return JsonConvert.SerializeObject(
                report ?? new DiagnosticReport(),
                new JsonSerializerSettings
                {
                    Formatting = formatting,
                    Converters = { new StringEnumConverter() }
                });
        }

        public static void CopyToClipboard(DiagnosticReport report)
        {
            GUIUtility.systemCopyBuffer = ToJson(report);
        }
    }
}
