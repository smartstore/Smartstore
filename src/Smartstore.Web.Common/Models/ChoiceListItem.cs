using System.Text.Json.Serialization;

namespace Smartstore.Web.Models
{
    /// <summary>
    /// Represents a selectable list item.
    /// It can be used together with smartstore.selectwrapper.js to create an extended select list.
    /// </summary>
    public class ChoiceListItem
    {
        /// <summary>
        /// The ID value, e.g. an entity ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Displayed text.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }

        /// <summary>
        /// A value indicating whether the item is selected.
        /// </summary>
        [JsonPropertyName("selected")]
        public bool Selected { get; set; }

        /// <summary>
        /// A value indicating whether the item is disabled.
        /// </summary>
        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; }

        /// <summary>
        /// Optional description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// Optional item hint, e.g. the product SKU.
        /// </summary>
        [JsonPropertyName("hint")]
        public string Hint { get; set; }

        /// <summary>
        /// Optional item title (applied as HTML attribute).
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// URL to add a link to the list item.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// Optional item link title (applied as HTML attribute).
        /// </summary>
        [JsonPropertyName("urlTitle")]
        public string UrlTitle { get; set; }

        /// <summary>
        /// Optional CSS classes for the choice options.
        /// </summary>
        [JsonPropertyName("cssClass")]
        public string CssClass { get; set; }
    }
}
