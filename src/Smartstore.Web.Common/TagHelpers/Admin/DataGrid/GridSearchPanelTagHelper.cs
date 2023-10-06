using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    /// <summary>
    /// Template for the left-hand grid search/filter form as Vue slot template. Passed object provides following members:
    /// <code>
    /// {
    ///     options,
    ///     dataSource,
    ///     columns,
    ///     paging,
    ///     sorting,
    ///     filtering,
    ///     grid: {
    ///         command,
    ///         rows,
    ///         editing
    ///     }
    /// }
    /// </code>
    /// </summary>
    [HtmlTargetElement("search-panel", ParentTag = "datagrid")]
    public class GridSearchPanelTagHelper : TagHelper
    {
        const string PreserveStateAttributeName = "preserve-state";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.SearchPanel = this;
            }
        }

        /// <summary>
        /// Whether to preserve the search form's state across requests in localStorage. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(PreserveStateAttributeName)]
        public bool PreserveState { get; set; } = true;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = await output.GetChildContentAsync();
            if (content.IsEmptyOrWhiteSpace)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "template";
            output.Attributes.Add("v-slot:search", "grid");
        }
    }
}
