using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Smartstore.Collections;
using Smartstore.Web.Rendering;
using Smartstore.Web.Rendering.Pager;

namespace Smartstore.Web.TagHelpers.Shared
{
    public enum PagerAlignment
    {
        Left,
        Centered,
        Right
    }

    public enum PagerSize
    {
        Mini,
        Small,
        Medium,
        Large
    }

    public enum PagerStyle
    {
        Pagination,
        Blog
    }

    [OutputElementHint("nav")]
    [HtmlTargetElement("pagination", Attributes = ListItemsAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class PaginationTagHelper : SmartTagHelper
    {
        const string ListItemsAttributeName = "sm-list-items";
        const string AlignmentAttributeName = "sm-alignment";
        const string SizeAttributeName = "sm-size";
        const string StyleAttributeName = "sm-style";
        const string ShowNavLabelAttributeName = "sm-show-nav-label";
        const string ShowFirstAttributeName = "sm-show-first";
        const string ShowLastAttributeName = "sm-show-last";
        const string ShowNextAttributeName = "sm-show-next";
        const string ShowPreviousAttributeName = "sm-show-previous";
        const string ShowPaginatorAttributeName = "sm-show-paginator";
        const string MaxPagesToDisplayAttributeName = "sm-max-pages";
        const string SkipActiveStateAttributeName = "sm-skip-active-state";
        const string ItemTitleFormatStringAttributeName = "sm-item-title-format-string";
        const string QueryParamNameAttributeName = "sm-query-param";

        [HtmlAttributeName(ListItemsAttributeName)]
        public IPageable ListItems { get; set; }

        [HtmlAttributeName(AlignmentAttributeName)]
        public PagerAlignment Alignment { get; set; } = PagerAlignment.Centered;

        [HtmlAttributeName(SizeAttributeName)]
        public PagerSize Size { get; set; } = PagerSize.Medium;

        [HtmlAttributeName(StyleAttributeName)]
        public PagerStyle Style { get; set; }

        [HtmlAttributeName(ShowNavLabelAttributeName)]
        public bool ShowNavLabel { get; set; }

        [HtmlAttributeName(ShowFirstAttributeName)]
        public bool ShowFirst { get; set; } = false;

        [HtmlAttributeName(ShowLastAttributeName)]
        public bool ShowLast { get; set; } = false;

        [HtmlAttributeName(ShowNextAttributeName)]
        public bool ShowNext { get; set; } = true;

        [HtmlAttributeName(ShowPreviousAttributeName)]
        public bool ShowPrevious { get; set; } = true;

        [HtmlAttributeName(ShowPaginatorAttributeName)]
        public bool ShowPaginator { get; set; } = true;

        [HtmlAttributeName(MaxPagesToDisplayAttributeName)]
        public int MaxPagesToDisplay { get; set; } = 7;

        [HtmlAttributeName(SkipActiveStateAttributeName)]
        public bool SkipActiveState { get; set; }

        [HtmlAttributeName(ItemTitleFormatStringAttributeName)]
        public string ItemTitleFormatString { get; set; }

        [HtmlAttributeName(QueryParamNameAttributeName)]
        public string QueryParamName { get; set; } = "page";

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            if (!ShowPaginator || ListItems == null || ListItems.TotalCount == 0 || ListItems.TotalPages <= 1)
            {
                output.SuppressOutput();
                return;
            }

            var items = CreateItemList();

            output.Attributes.Add("aria-label", "Page navigation");

            var itemsUl = new TagBuilder("ul");
            itemsUl.AppendCssClass("pagination mb-0");

            // Size
            if (Size == PagerSize.Large)
            {
                itemsUl.AppendCssClass("pagination-lg");
            }
            else if (Size == PagerSize.Small)
            {
                itemsUl.AppendCssClass("pagination-sm");
            }
            else if (Size == PagerSize.Mini)
            {
                itemsUl.AppendCssClass("pagination-xs");
            }

            // Alignment
            if (Alignment == PagerAlignment.Centered)
            {
                itemsUl.AppendCssClass("justify-content-center");
                output.AppendCssClass("text-center");
            }
            else if (Alignment == PagerAlignment.Right)
            {
                itemsUl.AppendCssClass("justify-content-end");
                output.AppendCssClass("text-right");
            }

            foreach (var item in items)
            {
                AppendItem(itemsUl, item);
            }

            output.TagName = "nav";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.Content.AppendHtml(itemsUl);
        }

        /// <summary>
        /// Creates complete item list by adding navigation items.
        /// </summary>
        protected virtual List<PagerItem> CreateItemList()
        {
            if (!ShowPaginator || ListItems.TotalPages <= 1)
            {
                return new List<PagerItem>();
            }

            // INFO: behaves like DataGrid's paginator.

            var currentIndex = ListItems.PageNumber;
            var totalPages = ListItems.TotalPages;
            var maxPages = MaxPagesToDisplay;
            var start = 1;

            if (currentIndex > maxPages)
            {
                var v = currentIndex % maxPages;
                start = v == 0 ? currentIndex - maxPages + 1 : currentIndex - v + 1;
            }

            var p = start + maxPages - 1;
            p = Math.Min(p, totalPages);

            var items = new List<PagerItem>();

            // First link
            if (ShowFirst)
            {
                items.Add(new PagerItem(1, T("Pager.First"), GenerateUrl(1), PagerItemType.FirstPage)
                {
                    State = (currentIndex > 1) ? PagerItemState.Normal : PagerItemState.Disabled
                });
            }

            // Previous link
            if (ShowPrevious)
            {
                items.Add(new PagerItem(currentIndex - 1, T("Pager.Previous"), GenerateUrl(currentIndex - 1), PagerItemType.PreviousPage)
                {
                    State = (currentIndex > 1) ? PagerItemState.Normal : PagerItemState.Disabled
                });
            }

            // Add the page number items.
            if (maxPages > 0)
            {
                AddPageItemsToList(items, start, p, currentIndex, totalPages);
            }

            // Next link.
            if (ShowNext)
            {
                items.Add(new PagerItem(currentIndex + 1, T("Pager.Next"), GenerateUrl(currentIndex + 1), PagerItemType.NextPage)
                {
                    State = (currentIndex == totalPages) ? PagerItemState.Disabled : PagerItemState.Normal,
                });
            }

            // Last link.
            if (ShowLast)
            {
                items.Add(new PagerItem(totalPages, T("Pager.Last"), GenerateUrl(totalPages), PagerItemType.LastPage)
                {
                    State = (currentIndex == totalPages) ? PagerItemState.Disabled : PagerItemState.Normal
                });
            }

            return items;
        }

        protected virtual void AddPageItemsToList(List<PagerItem> items, int start, int end, int currentIndex, int totalPages)
        {
            if (start > 1)
            {
                if (!ShowFirst)
                {
                    items.Add(new PagerItem(1, "1", GenerateUrl(1)));
                }
                items.Add(new PagerItem(start - 1, "...", GenerateUrl(start - 1)));
            }

            for (var i = start; i <= end; i++)
            {
                items.Add(new PagerItem(i, i.ToString(), GenerateUrl(i))
                {
                    State = (i == currentIndex && !SkipActiveState) ? PagerItemState.Selected : PagerItemState.Normal
                });
            }

            if (end < totalPages)
            {
                items.Add(new PagerItem(end + 1, "...", GenerateUrl(end + 1)));
                if (!ShowLast)
                {
                    items.Add(new PagerItem(totalPages, totalPages.ToString(), GenerateUrl(totalPages)));
                }
            }
        }

        /// <summary>
        /// Creates li tag from <see cref="PagerItem"/> and appends it to ul tag.
        /// </summary>
        protected virtual void AppendItem(TagBuilder itemsUl, PagerItem item)
        {
            var itemLi = new TagBuilder("li");

            using var classList = itemLi.GetClassList();

            classList.Add("page-item");

            if (item.IsNavButton)
            {
                classList.Add("page-item-nav");
                if (item.Type == PagerItemType.PreviousPage)
                {
                    classList.Add("prev");
                }
                else if (item.Type == PagerItemType.FirstPage)
                {
                    classList.Add("first");
                }
                else if (item.Type == PagerItemType.NextPage)
                {
                    classList.Add("next");
                }
                else if (item.Type == PagerItemType.LastPage)
                {
                    classList.Add("last");
                }
            }

            if (item.State == PagerItemState.Disabled)
            {
                classList.Add("disabled");
            }
            else if (item.State == PagerItemState.Selected)
            {
                classList.Add("active");
            }

            if (item.Type == PagerItemType.Text)
            {
                classList.Add("shrinked");
            }

            if (item.CssClass.HasValue())
            {
                classList.Add(item.CssClass.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            // Dispose here to write all collected classes into tag.
            classList.Dispose();

            var innerAOrSpan = new TagBuilder(item.Type == PagerItemType.Page || item.IsNavButton ? "a" : "span");

            if (item.Type == PagerItemType.Page || item.IsNavButton)
            {
                innerAOrSpan.Attributes.Add("href", item.Url);

                if (item.IsNavButton)
                {
                    innerAOrSpan.Attributes.Add("title", item.Text.AttributeEncode());
                    innerAOrSpan.Attributes.Add("aria-label", item.Text.AttributeEncode());
                    innerAOrSpan.Attributes.Add("tab-index", "-1");
                    if (Style != PagerStyle.Blog)
                    {
                        innerAOrSpan.Attributes.Add("rel", "tooltip");
                        innerAOrSpan.AddCssClass("page-nav");
                    }
                }
                else
                {
                    var formatStr = ItemTitleFormatString;
                    if (formatStr.HasValue())
                    {
                        innerAOrSpan.Attributes.Add("title", string.Format(formatStr, item.Text).AttributeEncode());
                        innerAOrSpan.Attributes.Add("rel", "tooltip");
                    }
                }
            }

            innerAOrSpan.AddCssClass("page-link");
            itemLi.InnerHtml.AppendHtml(GetItemInnerContent(item, innerAOrSpan));
            itemsUl.InnerHtml.AppendHtml(itemLi);
        }

        /// <summary>
        /// Sets inner content for for pager item.
        /// </summary>
        protected virtual TagBuilder GetItemInnerContent(PagerItem item, TagBuilder innerAOrSpan)
        {
            var iconI = new TagBuilder("i");

            switch (item.Type)
            {
                case PagerItemType.FirstPage:
                    iconI.AddCssClass("fa fa-angle-double-left");
                    break;
                case PagerItemType.PreviousPage:
                    iconI.AddCssClass("fa fa-angle-left");
                    break;
                case PagerItemType.NextPage:
                    iconI.AddCssClass("fa fa-angle-right");
                    break;
                case PagerItemType.LastPage:
                    iconI.AddCssClass("fa fa-angle-double-right");
                    break;
                default:
                    innerAOrSpan.InnerHtml.AppendHtml(item.Text);
                    break;
            }

            if (item.IsNavButton)
            {
                if (ShowNavLabel && item.Type is (PagerItemType.LastPage or PagerItemType.NextPage))
                {
                    innerAOrSpan.InnerHtml.AppendHtml($"<span class='pr-1'>{item.Text}</span>");
                }
                
                innerAOrSpan.InnerHtml.AppendHtml(iconI);

                if (ShowNavLabel && item.Type is (PagerItemType.FirstPage or PagerItemType.PreviousPage))
                {
                    innerAOrSpan.InnerHtml.AppendHtml($"<span class='pl-1'>{item.Text}</span>");
                }
            }

            return innerAOrSpan;
        }

        /// <summary>
        /// Generates and returns URL for pager item.
        /// </summary>
        protected virtual string GenerateUrl(int pageNumber)
        {
            var routeValues = ActionContextAccessor.ActionContext.RouteData.Values;
            var newValues = new RouteValueDictionary(routeValues)
            {
                [QueryParamName.NullEmpty() ?? "page"] = pageNumber
            };
            
            var query = ActionContextAccessor.ActionContext.HttpContext.Request.Query;
            if (query != null && query.Count > 0)
            {
                foreach (var item in query)
                {
                    if (!newValues.ContainsKey(item.Key))
                    {
                        newValues.Add(item.Key, item.Value);
                    }
                }
            }

            return UrlHelper.RouteUrl(newValues);
        }
    }
}
