using System.Text.Json.Serialization;

namespace Smartstore.Core.Rules.Rendering
{
    /// <summary>
    /// Represents a select list option for rules.
    /// </summary>
    public class RuleSelectItem
    {
        /// <summary>
        /// Value.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Displayed text.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }

        /// <summary>
        /// Option hint, e.g. the product SKU.
        /// </summary>
        [JsonPropertyName("hint")]
        public string Hint { get; set; }

        /// <summary>
        /// Whether the item is selected.
        /// </summary>
        [JsonPropertyName("selected")]
        public bool Selected { get; set; }
    }
}
