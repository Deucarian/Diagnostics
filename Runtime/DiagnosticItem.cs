using Newtonsoft.Json;

namespace Deucarian.Diagnostics
{
    public sealed class DiagnosticItem
    {
        public DiagnosticItem()
        {
        }

        public DiagnosticItem(string key, string label, string value, DiagnosticSeverity severity = DiagnosticSeverity.Info)
        {
            Key = key;
            Label = label;
            Value = value;
            Severity = severity;
        }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("severity")]
        public DiagnosticSeverity Severity { get; set; }
    }
}
