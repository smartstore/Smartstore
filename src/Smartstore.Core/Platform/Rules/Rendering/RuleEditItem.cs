using Newtonsoft.Json;

namespace Smartstore.Core.Rules.Rendering
{
    /// <summary>
    /// Represents an edit item for rules.
    /// </summary>
    public class RuleEditItem
    {
        [JsonProperty("ruleId")]
        public int RuleId { get; set; }

        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }
}
