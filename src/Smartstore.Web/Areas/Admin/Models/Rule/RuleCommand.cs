using Newtonsoft.Json;
using Smartstore.Core.Rules;

namespace Smartstore.Admin.Models.Rules
{
    [Serializable]
    public class RuleCommand
    {
        [JsonProperty("scope")]
        public RuleScope Scope { get; set; }

        [JsonProperty("ruleSetId")]
        public int RuleSetId { get; set; }

        [JsonProperty("ruleId")]
        public int RuleId { get; set; }

        [JsonProperty("relatedId")]
        public int RelatedId { get; set; }

        [JsonProperty("ruleType")]
        public string RuleType { get; set; }

        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("ruleData")]
        public RuleEditItem[] RuleData { get; set; }
    }
}
