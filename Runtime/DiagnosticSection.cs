using System.Collections.Generic;
using Newtonsoft.Json;

namespace Deucarian.Diagnostics
{
    public sealed class DiagnosticSection
    {
        public DiagnosticSection()
        {
            Items = new List<DiagnosticItem>();
        }

        public DiagnosticSection(string id, string title)
            : this()
        {
            Id = id;
            Title = title;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("severity")]
        public DiagnosticSeverity Severity
        {
            get { return DiagnosticSeverityUtility.Aggregate(Items); }
        }

        [JsonProperty("items")]
        public List<DiagnosticItem> Items { get; set; }

        public DiagnosticSection AddItem(string key,
                                         string label,
                                         string value,
                                         DiagnosticSeverity severity = DiagnosticSeverity.Info,
                                         string message = null)
        {
            Items.Add(new DiagnosticItem(key, label, value, severity)
            {
                Message = message
            });
            return this;
        }
    }
}
