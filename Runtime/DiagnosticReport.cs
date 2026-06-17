using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Deucarian.Diagnostics
{
    public sealed class DiagnosticReport
    {
        public DiagnosticReport()
        {
            GeneratedAtUtc = DateTime.UtcNow;
            Sections = new List<DiagnosticSection>();
        }

        [JsonProperty("generated_at_utc")]
        public DateTime GeneratedAtUtc { get; set; }

        [JsonProperty("severity")]
        public DiagnosticSeverity Severity
        {
            get { return DiagnosticSeverityUtility.Aggregate(Sections); }
        }

        [JsonProperty("sections")]
        public List<DiagnosticSection> Sections { get; set; }
    }
}
