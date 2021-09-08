using Newtonsoft.Json;

namespace Smartstore.Web.Models
{
    /// <summary>
    /// Represents a generic treeview item.
    /// It can be used together with smartstore.tree.js to load tree nodes using AJAX.
    /// </summary>
    public partial class TreeItem
    {
        /// <summary>
        /// The name of the node.
        /// </summary>
        public string Name { get; set; }

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
        /// Number of child elements. Is appended to <see cref="Name"/> if greater than zero.
        /// </summary>
        public int NumChildren { get; set; }

        /// <summary>
        /// Number of all child elements.
        /// </summary>
        public int NumChildrenDeep { get; set; }

        /// <summary>
        /// A value indicating whether the node is enabled.
        /// Disabled nodes cannot be checked or selected.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// A value indicating whether to display the node dimmed.
        /// </summary>
        public bool Dimmed { get; set; }

        /// <summary>
        /// A value indicating whether the node is checked.
        /// </summary>
        public bool Checked { get; set; }

        /// <summary>
        /// URL to open when clicking the node.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }

        /// <summary>
        /// HTML target attribute for <see cref="Url"/>.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string UrlTarget { get; set; }

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

        /// <summary>
        /// Optional icon URL.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string IconUrl { get; set; }
    }
}
