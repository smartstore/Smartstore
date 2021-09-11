using System.Collections.Generic;
using Smartstore.Core.Configuration;
using Smartstore.Core.Search;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Settings
{
    public class ForumSearchSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the search mode.
        /// </summary>
        public SearchMode SearchMode { get; set; } = SearchMode.Contains;

        /// <summary>
        /// Gets or sets the name of fields to be searched. The name field is always searched.
        /// </summary>
        public List<string> SearchFields { get; set; } = new List<string> { "username", "text" };

        /// <summary>
        /// Gets or sets the default sort order in search results.
        /// </summary>
        public ForumTopicSorting DefaultSortOrder { get; set; } = ForumTopicSorting.Relevance;

        /// <summary>
        /// Gets or sets a value indicating whether instant-search is enabled.
        /// </summary>
        public bool InstantSearchEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of hits to return when using "instant-search" feature.
        /// </summary>
        public int InstantSearchNumberOfHits { get; set; } = 10;

        /// <summary>
        /// Gets or sets a minimum instant-search term length.
        /// </summary>
        public int InstantSearchTermMinLength { get; set; } = 2;

        /// <summary>
        /// Gets or sets the minimum hit count for a filter value. Values with a lower hit count are not displayed.
        /// </summary>
        public int FilterMinHitCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum number of filter values to be displayed.
        /// </summary>
        public int FilterMaxChoicesCount { get; set; } = 20;

        #region Common facet settings

        public bool ForumDisabled { get; set; }
        public bool CustomerDisabled { get; set; }
        public bool DateDisabled { get; set; }

        public int ForumDisplayOrder { get; set; } = 1;
        public int CustomerDisplayOrder { get; set; } = 2;
        public int DateDisplayOrder { get; set; } = 3;

        #endregion
    }
}
