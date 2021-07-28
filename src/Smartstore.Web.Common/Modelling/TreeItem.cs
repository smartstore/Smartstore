using Newtonsoft.Json;

namespace Smartstore.Web.Modelling
{
    /// <summary>
    /// Represents a generic treeview item.
    /// It can be used together with smartstore.tree.js to load tree nodes using AJAX.
    /// </summary>
    public class TreeItem
    {
        /// <summary>
        /// The display name of the node.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The title of the node. It is used for the HTML title attribute.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        /// <summary>
        /// Optional text of the badge.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string BadgeText { get; set; }

        /// <summary>
        /// Optional CSS class of the badge, e.g. badge-secondary.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string BadgeStyle { get; set; }

        /// <summary>
        /// A value indicating whether the node is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// A value indicating whether the node is published.
        /// Unpublished nodes are diplayed muted.
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// URL to open when clicking the node.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }

        /// <summary>
        /// State ID of the node. Needs to be unique across the entire tree.
        /// It is used for ID and name attribute of the state checkbox (if any).
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string StateId { get; set; }

        /// <summary>
        /// The value of the state checkbox.
        /// It is used for value attribute of the state checkbox (if any).
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string StateValue { get; set; }

        /// <summary>
        /// Optional icon class.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string IconClass { get; set; }
    }
}
