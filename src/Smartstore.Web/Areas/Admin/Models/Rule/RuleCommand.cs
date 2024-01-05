using Newtonsoft.Json;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Admin.Models.Rules
{
    [Serializable]
    public class RuleCommand
    {
        [JsonProperty("scope")]
        public RuleScope Scope { get; set; }

        /// <summary>
        /// Identifier of the related entity.
        /// <see cref="ProductVariantAttribute.Id"/> if <see cref="Scope"/> == <see cref="RuleScope.ProductAttribute"/>.
        /// </summary>
        [JsonProperty("entityId")]
        public int? EntityId { get; set; }

        [JsonProperty("ruleSetId")]
        public int RuleSetId { get; set; }

        [JsonProperty("ruleId")]
        public int RuleId { get; set; }

        [JsonProperty("ruleType")]
        public string RuleType { get; set; }

        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("ruleData")]
        public RuleEditItem[] RuleData { get; set; }
    }
}
