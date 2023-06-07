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
        /// Creates complete item list by adding navigation items for.
        /// </summary>
        /// <returns></returns>
        protected List<PagerItem> CreateItemList()
        {
            if (!ShowPaginator || ListItems.TotalPages <= 1)
            {
                return new List<PagerItem>();
            }   

            var pageNumber = ListItems.PageNumber;
            var pageCount = ListItems.TotalPages;

            var results = new List<PagerItem>();

            PagerItem item;

            // First link
            if (ShowFirst)
            {
                item = new PagerItem(1, T("Pager.First"), GenerateUrl(1), PagerItemType.FirstPage)
                {
                    State = (pageNumber > 1) ? PagerItemState.Normal : PagerItemState.Disabled
                };
                results.Add(item);
            }

            // Previous link
            if (ShowPrevious)
            {
                item = new PagerItem(pageNumber - 1, T("Pager.Previous"), GenerateUrl(pageNumber - 1), PagerItemType.PreviousPage)
                {
                    State = (pageNumber > 1) ? PagerItemState.Normal : PagerItemState.Disabled
                };
                results.Add(item);
            }

            // Add the page number items.
            if (MaxPagesToDisplay > 0)
            {
                AddPageItemsList(results);
            }

            // Next link.
            var hasNext = false;
            if (ShowNext)
            {
                item = new PagerItem(pageNumber + 1, T("Pager.Next"), GenerateUrl(pageNumber + 1), PagerItemType.NextPage)
                {
                    State = (pageNumber == pageCount) ? PagerItemState.Disabled : PagerItemState.Normal
                };
                results.Add(item);
                hasNext = true;
            }

            // Last link.
            if (ShowLast)
            {
                item = new PagerItem(pageCount, T("Pager.Last"), GenerateUrl(pageCount), PagerItemType.LastPage)
                {
                    State = (pageNumber == pageCount) ? PagerItemState.Disabled : PagerItemState.Normal
                };
                if (Style == PagerStyle.Pagination || !hasNext)
                {
                    results.Add(item);
                }
                else
                {
                    // BlogStyle Last-Item is right-aligned, so shift left.
                    results.Insert(results.Count - 1, item);
                }
            }

            return results;
        }

        /// <summary>
        /// Can be overridden in a custom renderer in order to apply a custom numbering sequence.
        /// </summary>
        /// <param name="items"></param>
        protected virtual void AddPageItemsList(List<PagerItem> items)
        {
            var pageNumber = ListItems.PageNumber;
            var pageCount = ListItems.TotalPages;

            var (start, end) = CalculateIndexes(out var startEllipsis, out var endEllipsis);

            if (startEllipsis)
            {
                items.Add(new PagerItem(1, "1", GenerateUrl(1)));
                items.Add(new PagerItem(-1, "...", "", PagerItemType.Text));
            }

            for (int i = start; i <= end; i++)
            {
                var item = new PagerItem(i, i.ToString(), GenerateUrl(i));
                if (i == pageNumber && !SkipActiveState)
                {
                    item.State = PagerItemState.Selected;
                }
                items.Add(item);
            }

            if (endEllipsis)
            {
                items.Add(new PagerItem(-1, "...", "", PagerItemType.Text));
                items.Add(new PagerItem(pageCount, pageCount.ToString(), GenerateUrl(pageCount)));
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

        protected virtual (int start, int end) CalculateIndexes(out bool startEllipsis, out bool endEllipsis)
        {
            var items = ListItems;
            var currentIndex = items.PageNumber;
            var start = 1;
            var end = items.TotalPages;

            startEllipsis = false;
            endEllipsis = false;

            if (items.TotalPages > MaxPagesToDisplay + 1)
            {
                // Tolerate +1 to spare ellipsis

                var half = MaxPagesToDisplay / 2;

                start = currentIndex - half;
                end = currentIndex + half;

                if (start <= 1)
                {
                    start = 1;
                    end = MaxPagesToDisplay;
                }
                else if (end >= items.TotalPages)
                {
                    end = items.TotalPages;
                    start = end - MaxPagesToDisplay;
                }

                // Check if ellipsis items should be displayed
                if (!ShowFirst && currentIndex > 3)
                {
                    startEllipsis = true;
                    start += 1;
                }

                if (!ShowLast && currentIndex < (items.TotalPages - 3))
                {
                    endEllipsis = true;
                    end -= 1;
                }
            }

            return (start, end);
        }

        /// <summary>
        /// Gets first individual page index.
        /// </summary>
        /// <returns>Page index</returns>
        protected virtual int GetFirstPageIndex()
        {
            if ((ListItems.TotalPages < MaxPagesToDisplay) || ((ListItems.PageIndex - (MaxPagesToDisplay / 2)) < 0))
            {
                return 0;
            }

            if ((ListItems.PageIndex + (MaxPagesToDisplay / 2)) >= ListItems.TotalPages)
            {
                return ListItems.TotalPages - MaxPagesToDisplay;
            }

            return ListItems.PageIndex - (MaxPagesToDisplay / 2);
        }

        ///// <summary>
        ///// Get last individual page index.
        ///// </summary>
        ///// <returns>Page index</returns>
        //protected virtual int GetLastPageIndex()
        //{
        //    int num = MaxPagesToDisplay / 2;
        //    if ((MaxPagesToDisplay % 2) == 0)
        //    {
        //        num--;
        //    }

        //    if ((ListItems.TotalPages < MaxPagesToDisplay) || ((ListItems.PageIndex + num) >= ListItems.TotalPages))
        //    {
        //        return ListItems.TotalPages - 1;
        //    }

        //    if ((ListItems.PageIndex - (MaxPagesToDisplay / 2)) < 0)
        //    {
        //        return MaxPagesToDisplay - 1;
        //    }

        //    return ListItems.PageIndex + num;
        //}

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
                    if (!newValues.Keys.Contains(item.Key))
                    {
                        newValues.Add(item.Key, item.Value);
                    }
                }
            }

            return UrlHelper.RouteUrl(newValues);
        }
    }
}
