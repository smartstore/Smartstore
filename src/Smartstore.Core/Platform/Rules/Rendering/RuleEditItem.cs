using System.Text.Json.Serialization;

namespace Smartstore.Core.Rules.Rendering
{
    /// <summary>
    /// Represents an edit item for rules.
    /// </summary>
    public class RuleEditItem
    {
        [JsonPropertyName("ruleId")]
        public int RuleId { get; set; }

        [JsonPropertyName("op")]
        public string Op { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }
    }
}
