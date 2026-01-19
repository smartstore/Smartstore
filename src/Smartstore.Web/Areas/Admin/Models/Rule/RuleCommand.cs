
using System.Text.Json.Serialization;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Admin.Models.Rules
{
    public class RuleCommand
    {
        [JsonPropertyName("scope")]
        public RuleScope Scope { get; set; }

        /// <summary>
        /// Identifier of the related entity.
        /// <see cref="ProductVariantAttribute.Id"/> if <see cref="Scope"/> == <see cref="RuleScope.ProductAttribute"/>.
        /// </summary>
        [JsonPropertyName("entityId")]
        public int? EntityId { get; set; }

        [JsonPropertyName("ruleSetId")]
        public int RuleSetId { get; set; }

        [JsonPropertyName("ruleId")]
        public int RuleId { get; set; }

        [JsonPropertyName("ruleType")]
        public string RuleType { get; set; }

        [JsonPropertyName("op")]
        public string Op { get; set; }

        [JsonPropertyName("ruleData")]
        public RuleEditItem[] RuleData { get; set; }
    }
}
