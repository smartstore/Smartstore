using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
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
        const string ContentClassNameAttribute = "sm-content-class";
        const string QueryParamNameAttributeName = "sm-query-param";
        const string ContentTargetNameAttribute = "sm-content-target";
        const string UrlNameAttribute = "sm-url";

        [HtmlAttributeName(ListItemsAttributeName)]
        public IPageable ListItems { get; set; }

        [HtmlAttributeName(AlignmentAttributeName)]
        public PagerAlignment Alignment { get; set; } = PagerAlignment.Centered;

        [HtmlAttributeName(SizeAttributeName)]
        public PagerSize Size { get; set; } = PagerSize.Medium;

        [HtmlAttributeName(StyleAttributeName)]
        public PagerStyle Style { get; set; }

        /// <summary>
        /// Shows labels for the chevron buttons, but only on small devices (>= sm).
        /// </summary>
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

        /// <summary>
        /// The max number of pages to render. Odd values are recommended: 7, 9, 11 (default), 13, ...
        /// </summary>
        [HtmlAttributeName(MaxPagesToDisplayAttributeName)]
        public int MaxPagesToDisplay { get; set; } = 11; // First + ... + 3xp + CURRENT + 3xp + ... + Last = 11

        [HtmlAttributeName(SkipActiveStateAttributeName)]
        public bool SkipActiveState { get; set; }

        [HtmlAttributeName(ItemTitleFormatStringAttributeName)]
        public string ItemTitleFormatString { get; set; }

        [HtmlAttributeName(ContentClassNameAttribute)]
        public string ContentCssClass { get; set; }

        [HtmlAttributeName(QueryParamNameAttributeName)]
        public string QueryParamName { get; set; } = "page";

        /// <summary>
        /// Gets or sets URL for pager items.
        /// If <c>null</c>, URL will be obtained from HTTP request.
        /// </summary>
        [HtmlAttributeName(UrlNameAttribute)]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets a HTML selector to apply asynchronously loaded content using AJAX.
        /// If empty, the content will be loaded synchronously.
        /// </summary>
        [HtmlAttributeName(ContentTargetNameAttribute)]
        public string ContentTarget { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            if (!ShowPaginator || ListItems == null || ListItems.TotalCount == 0 || ListItems.TotalPages <= 1)
            {
                output.SuppressOutput();
                return;
            }

            if (QueryParamName.IsEmpty())
            {
                QueryParamName = "page";
            }

            var items = CreateItemList();

            output.Attributes.Add("aria-label", "Page navigation");
            output.AppendCssClass("pagination-container");

            if (ContentTarget.HasValue())
            {
                output.Attributes.Add("data-target", ContentTarget);
            }

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
            else
            {
                itemsUl.AppendCssClass("pagination-md");
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

            if (ContentCssClass.HasValue())
            {
                itemsUl.AppendCssClass(ContentCssClass);
            }

            var itemsCount = items.Count;
            foreach (var item in items)
            {
                AppendItem(itemsUl, item, itemsCount);
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
                return [];
            }

            var currentPage = ListItems.PageNumber;
            var totalPages = ListItems.TotalPages;
            var maxPages = Math.Max(5, MaxPagesToDisplay); // Cannot handle less than 5 items: First + ... + CURRENT + ... + Last

            var items = new List<PagerItem>();

            // First link
            if (ShowFirst)
            {
                items.Add(new PagerItem(1, T("Pager.First"), GenerateUrl(1), PagerItemType.FirstPage)
                {
                    State = (currentPage > 1) ? PagerItemState.Normal : PagerItemState.Disabled,
                    DisplayBreakpointUp = maxPages > 0 ? "md" : null
                });
            }

            // Previous link
            if (ShowPrevious)
            {
                items.Add(new PagerItem(currentPage - 1, T("Pager.Previous"), GenerateUrl(currentPage - 1), PagerItemType.PreviousPage)
                {
                    State = (currentPage > 1) ? PagerItemState.Normal : PagerItemState.Disabled
                });
            }

            // Add the page number items.
            if (maxPages > 0)
            {
                var numPages = Math.Min(totalPages, maxPages);
                
                // xl range
                var (start, end) = CalculateItemRange(maxPages);

                var hasStartGap = start > 1;
                var hasEndGap = end < totalPages;

                // <= lg range (max 9 items)
                var (startLg, endLg) = numPages > 9 ? CalculateItemRange(9, hasStartGap, hasEndGap) : (start, end);

                // <= sm range (max 7 items)
                var (startSm, endSm) = numPages > 7 ? CalculateItemRange(7, hasStartGap, hasEndGap) : (startLg, endLg);

                // xs range (max 5 items)
                var (startXs, endXs) = numPages > 5 ? CalculateItemRange(5, hasStartGap, hasEndGap) : (startSm, endSm);

                if (start > 1)
                {
                    // Display first page
                    items.Add(new PagerItem(1, "1", GenerateUrl(1)));

                    if (hasStartGap)
                    {
                        // Display a gap at the start if necessary
                        items.Add(new PagerItem(start - 1, "...", GenerateUrl(start - 1), PagerItemType.Gap));
                    }
                }

                for (int i = start; i <= end; i++)
                {
                    var displayBreakpointUp = (string)null;

                    // Handle responsiveness..
                    if (i != currentPage && i > 1 && i < totalPages)
                    {
                        // ...by hiding too distant pages. Current, first and last pages are always visible.
                        if (i < startLg || i > endLg)
                        {
                            displayBreakpointUp = "xl";
                        }
                        else if (i < startSm || i > endSm)
                        {
                            displayBreakpointUp = "md";
                        }
                        else if (i < startXs || i > endXs)
                        {
                            displayBreakpointUp = "sm";
                        }
                    }

                    var state = PagerItemState.Normal;
                    if (!SkipActiveState && (i == currentPage || (currentPage <= 0 && i == 1)))
                    {
                        state = PagerItemState.Selected;
                    }

                    items.Add(new PagerItem(i, i.ToString(), GenerateUrl(i))
                    {
                        State = state,
                        DisplayBreakpointUp = displayBreakpointUp
                    });
                }

                if (hasEndGap)
                {
                    // Display a gap at the end if necessary
                    items.Add(new PagerItem(end + 1, "...", GenerateUrl(end + 1), PagerItemType.Gap));

                    if (end < totalPages - 2)
                    {
                        // Always display the last page if not already displayed
                        items.Add(new PagerItem(totalPages, totalPages.ToString(), GenerateUrl(totalPages)));
                    }
                }
            }

            // Next link.
            if (ShowNext)
            {
                items.Add(new PagerItem(currentPage + 1, T("Pager.Next"), GenerateUrl(currentPage + 1), PagerItemType.NextPage)
                {
                    State = (currentPage == totalPages) ? PagerItemState.Disabled : PagerItemState.Normal,
                });
            }

            // Last link.
            if (ShowLast)
            {
                items.Add(new PagerItem(totalPages, T("Pager.Last"), GenerateUrl(totalPages), PagerItemType.LastPage)
                {
                    State = (currentPage == totalPages) ? PagerItemState.Disabled : PagerItemState.Normal,
                    DisplayBreakpointUp = maxPages > 0 ? "md" : null
                });
            }

            return items;
        }

        /// <summary>
        /// Calculates the start...end range.
        /// </summary>
        private (int start, int end) CalculateItemRange(int maxPages, bool? hasStartGap = null, bool? hasEndGap = null)
        {
            var totalPages = ListItems.TotalPages;
            var currentPage = ListItems.PageNumber;
            var start = 1;
            var end = totalPages;

            if (totalPages > maxPages)
            {
                var middle = (int)Math.Ceiling(maxPages / 2d) - 1;
                var below = (currentPage - middle);
                var above = (currentPage + middle);

                if (below < 2)
                {
                    above = maxPages;
                    below = 1;
                }
                else if (above > (totalPages - 2))
                {
                    above = totalPages;
                    below = totalPages - maxPages + 1;
                }

                start = below;
                end = above;
            }

            // INFO: code can be shorter, but this way it is more readable
            if (hasStartGap.HasValue)
            {
                // A subsequent call to lower (< xl) tiers
                if (hasStartGap == true)
                {
                    // The main tier has a gap (1 & ...). Offset by 2.
                    start += 2;
                }
                else if (start > 1)
                {
                    // The main tier has no gap. Offset by 1, because of the first page that is also rendered.
                    start++;
                }
            }
            else
            {
                // The main (xl) call
                if (start > 1)
                {
                    // The main tier has a gap (1 & ...). Offset by 2.
                    start += 2;
                }
            }

            if (hasEndGap.HasValue)
            {
                // A subsequent call to lower (< xl) tiers
                if (hasEndGap == true)
                {
                    // The main tier has a gap (... & LastPage). Offset by 2.
                    end -= 2;
                }
                else if (end < totalPages)
                {
                    // The main tier has no gap. Offset by 1, because of the last page that is also rendered.
                    end--;
                }
                else if (currentPage == start - 1)
                {
                    // The main tier has no gap, but the current page comes right before the start page.
                    // Offset by 2 because the last AND current pages need to be rendered.
                    end -= 2;
                }
            }
            else
            {
                // The main (xl) call
                if (end < totalPages)
                {
                    // The main tier has a gap (... & LastPage). Offset by 2.
                    end -= 2;
                }
            }

            return (start, end);
        }

        /// <summary>
        /// Creates li tag from <see cref="PagerItem"/> and appends it to ul tag.
        /// </summary>
        protected virtual void AppendItem(TagBuilder itemsUl, PagerItem item, int itemsCount)
        {
            var itemLi = new TagBuilder("li");

            using var classList = itemLi.GetClassList();

            classList.Add("page-item");

            if (item.IsNavButton)
            {
                classList.Add("page-item-nav");
                if (item.Type == PagerItemType.PreviousPage)
                {
                    classList.Add("prev", "back");
                }
                else if (item.Type == PagerItemType.FirstPage)
                {
                    classList.Add("first", "back");
                }
                else if (item.Type == PagerItemType.NextPage)
                {
                    classList.Add("next", "advance");
                }
                else if (item.Type == PagerItemType.LastPage)
                {
                    classList.Add("last", "advance");
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
            else if (item.Type == PagerItemType.Gap)
            {
                classList.Add("gap");
            }

            if (item.CssClass.HasValue())
            {
                classList.Add(item.CssClass.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            var isResponsive = itemsCount >= 6;
            if (isResponsive && item.DisplayBreakpointUp.HasValue())
            {
                classList.Add("d-none", $"d-{item.DisplayBreakpointUp}-inline-block");
            }

            // Dispose here to write all collected classes into tag.
            classList.Dispose();

            var isClickable = item.Type is PagerItemType.Page or PagerItemType.Gap;
            var innerAOrSpan = new TagBuilder(isClickable || item.IsNavButton ? "a" : "span");

            if (isClickable || item.IsNavButton)
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
                    innerAOrSpan.InnerHtml.AppendHtml($"<span class='nav-label d-sm-none'>{item.Text}</span>");
                }
                
                innerAOrSpan.InnerHtml.AppendHtml(iconI);

                if (ShowNavLabel && item.Type is (PagerItemType.FirstPage or PagerItemType.PreviousPage))
                {
                    innerAOrSpan.InnerHtml.AppendHtml($"<span class='nav-label d-sm-none'>{item.Text}</span>");
                }
            }

            return innerAOrSpan;
        }

        /// <summary>
        /// Generates and returns URL for pager item.
        /// </summary>
        protected virtual string GenerateUrl(int pageNumber)
        {
            var request = ViewContext.HttpContext.Request;

            // Resolve current path without query
            var path = Url.HasValue() ? new(Url) : (request.PathBase + request.Path);
            
            // Merge page index param with current query
            var queryPart = request.Query.Merge($"?{QueryParamName}={pageNumber}");

            // Append modified query to path
            var url = path.Add(queryPart);

            return url;
        }
    }
}
