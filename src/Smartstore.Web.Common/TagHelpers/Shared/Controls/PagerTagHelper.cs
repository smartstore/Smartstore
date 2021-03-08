using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering;
using Smartstore.Web.Rendering.Pager;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("nav")]
    [HtmlTargetElement("pager", Attributes = ListItemsAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class PagerTagHelper : SmartTagHelper 
    {
        const string ListItemsAttributeName = "sm-list-items";
        const string AlignmentAttributeName = "sm-alignment";
        const string SizeAttributeName = "sm-size";
        const string StyleAttributeName = "sm-style";
        const string ShowFirstAttributeName = "sm-show-first";
        const string ShowLastAttributeName = "sm-show-last";
        const string ShowNextAttributeName = "sm-show-next";
        const string ShowPreviousAttributeName = "sm-show-previous";
        const string ShowSummaryAttributeName = "sm-show-summary";
        const string ShowPaginatorAttributeName = "sm-show-paginator";
        const string MaxPagesToDisplayAttributeName = "sm-max-pages";
        const string SkipActiveStateAttributeName = "sm-skip-active-state";
        const string ItemTitleFormatStringAttributeName = "sm-item-title-format-string";

        // TODO: (mh) (core) Make ModifiedParam attribute?

        public PagerTagHelper()
        {
            Pager = new Pager();
            Pager.ModifiedParam.Name = "pagenumber";
        }

        [HtmlAttributeName(ListItemsAttributeName)]
        public IPageable ListItems { get; set; }

        [HtmlAttributeName(AlignmentAttributeName)]
        public PagerAlignment Alignment { get; set; } = PagerAlignment.Centered;

        [HtmlAttributeName(SizeAttributeName)]
        public PagerSize Size { get; set; } = PagerSize.Medium;

        [HtmlAttributeName(StyleAttributeName)]
        public PagerStyle Style { get; set; }

        [HtmlAttributeName(ShowFirstAttributeName)]
        public bool ShowFirst { get; set; } = false;

        [HtmlAttributeName(ShowLastAttributeName)]
        public bool ShowLast { get; set; } = false;

        [HtmlAttributeName(ShowNextAttributeName)]
        public bool ShowNext { get; set; } = true;

        [HtmlAttributeName(ShowPreviousAttributeName)]
        public bool ShowPrevious { get; set; } = true;

        [HtmlAttributeName(ShowSummaryAttributeName)]
        public bool ShowSummary { get; set; } = false;

        [HtmlAttributeName(ShowPaginatorAttributeName)]
        public bool ShowPaginator { get; set; } = true;

        [HtmlAttributeName(MaxPagesToDisplayAttributeName)]
        public int MaxPagesToDisplay { get; set; } = 8;

        [HtmlAttributeName(SkipActiveStateAttributeName)]
        public bool SkipActiveState { get; set; }

        [HtmlAttributeName(ItemTitleFormatStringAttributeName)]
        public string ItemTitleFormatString { get; set; }

        [HtmlAttributeNotBound]
        public Pager Pager { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();

            if (!ShowPaginator || ListItems == null || ListItems.TotalCount == 0 || ListItems.TotalPages <= 1)
            {
                return;
            }

            var items = CreateItemList();

            // Alignment
            if (Alignment == PagerAlignment.Right)
            {
                output.AppendCssClass("text-right");
            }
            else if (Alignment == PagerAlignment.Centered)
            {
                output.AppendCssClass("text-center");
            }

            output.Attributes.Add("aria-label", "Page navigation");

            if (ShowSummary && ListItems.TotalPages > 1)
            {
                var summaryDiv = new TagBuilder("div");
                summaryDiv.AppendCssClass("pagination-summary float-left");
                summaryDiv.InnerHtml.AppendHtml(Pager.CurrentPageText.FormatInvariant(ListItems.PageNumber, ListItems.TotalPages, ListItems.TotalCount));
                output.Content.AppendHtml(summaryDiv);
            }

            var itemsUl = new TagBuilder("ul");

            itemsUl.AppendCssClass(Style == PagerStyle.Pagination ? "pagination" : "pagination");

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

            // BS 4 alignment
            if (Alignment == PagerAlignment.Centered)
            {
                itemsUl.AppendCssClass("justify-content-center");
            }
            else if (Alignment == PagerAlignment.Right)
            {
                itemsUl.AppendCssClass("justify-content-end");
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
        /// Creates complete item list by adding navigation items for
        /// </summary>
        /// <returns></returns>
        protected List<PagerItem> CreateItemList()
        {
            if (!ShowPaginator || ListItems.TotalPages <= 1)
                return new List<PagerItem>();

            var pageNumber = ListItems.PageNumber;
            var pageCount = ListItems.TotalPages;

            var results = new List<PagerItem>();

            PagerItem item;

            // First link.
            if (ShowFirst && pageNumber > 1)
            {
                item = new PagerItem(Pager.FirstButtonText, GenerateUrl(1), PagerItemType.FirstPage)
                {
                    State = (pageNumber > 1) ? PagerItemState.Normal : PagerItemState.Disabled
                };
                results.Add(item);
            }

            // Previous link.
            if (ShowPrevious && pageNumber > 1)
            {
                item = new PagerItem(Pager.PreviousButtonText, GenerateUrl(pageNumber - 1), PagerItemType.PreviousPage)
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
            if (ShowNext && pageNumber < pageCount)
            {
                item = new PagerItem(Pager.NextButtonText, GenerateUrl(pageNumber + 1), PagerItemType.NextPage)
                {
                    State = (pageNumber == pageCount) ? PagerItemState.Disabled : PagerItemState.Normal
                };
                results.Add(item);
                hasNext = true;
            }

            // Last link.
            if (ShowLast && pageNumber < pageCount)
            {
                item = new PagerItem(Pager.LastButtonText, GenerateUrl(pageCount), PagerItemType.LastPage)
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

            int start = GetFirstPageIndex() + 1;
            int end = GetLastPageIndex() + 1;

            if (start > 3 && !ShowFirst)
            {
                items.Add(new PagerItem("1", GenerateUrl(1)));
                items.Add(new PagerItem("...", "", PagerItemType.Text));
            }

            for (int i = start; i <= end; i++)
            {
                var item = new PagerItem(i.ToString(), GenerateUrl(i));
                if (i == pageNumber && !SkipActiveState)
                {
                    item.State = PagerItemState.Selected;
                }
                items.Add(item);
            }

            if (end < (pageCount - 3) && !ShowLast)
            {
                items.Add(new PagerItem("...", "", PagerItemType.Text));
                items.Add(new PagerItem(pageCount.ToString(), GenerateUrl(pageCount)));
            }
        }

        /// <summary>
        /// Creates li tag from <see cref="PagerItem"/> and appends it to ul tag.
        /// </summary>
        protected virtual void AppendItem(TagBuilder itemsUl, PagerItem item)
        {
            var itemLi = new TagBuilder("li");

            if (item.State == PagerItemState.Disabled)
            {
                itemLi.AddCssClass("disabled");
            }
            else if (item.State == PagerItemState.Selected)
            {
                itemLi.AddCssClass("active");
            }

            if (item.Type == PagerItemType.Text)
            {
                itemLi.AddCssClass("shrinked");
            }

            if (Style == PagerStyle.Blog && item.IsNavButton)
            {
                itemLi.AddCssClass((item.Type == PagerItemType.PreviousPage || item.Type == PagerItemType.FirstPage) ? "previous" : "next");
            }

            itemLi.AddCssClass("page-item");
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
                innerAOrSpan.InnerHtml.AppendHtml(iconI);
            }

            return innerAOrSpan;
        }

        /// <summary>
        /// Gets first individual page index.
        /// </summary>
        /// <returns>Page index</returns>
        protected virtual int GetFirstPageIndex()
        {
            if ((ListItems.TotalPages < MaxPagesToDisplay) ||
                ((ListItems.PageIndex - (MaxPagesToDisplay / 2)) < 0))
            {
                return 0;
            }
            if ((ListItems.PageIndex + (MaxPagesToDisplay / 2)) >= ListItems.TotalPages)
            {
                return (ListItems.TotalPages - MaxPagesToDisplay);
            }
            return (ListItems.PageIndex - (MaxPagesToDisplay / 2));
        }

        /// <summary>
        /// Get last individual page index.
        /// </summary>
        /// <returns>Page index</returns>
        protected virtual int GetLastPageIndex()
        {
            int num = MaxPagesToDisplay / 2;
            if ((MaxPagesToDisplay % 2) == 0)
            {
                num--;
            }
            if ((ListItems.TotalPages < MaxPagesToDisplay) ||
                ((ListItems.PageIndex + num) >= ListItems.TotalPages))
            {
                return (ListItems.TotalPages - 1);
            }
            if ((ListItems.PageIndex - (MaxPagesToDisplay / 2)) < 0)
            {
                return (MaxPagesToDisplay - 1);
            }
            return (ListItems.PageIndex + num);
        }

        /// <summary>
        /// Generates and returns URL for pager item.
        /// </summary>
        protected virtual string GenerateUrl(int pageNumber)
        {
            //// paramName is "page" by default, but could also be "pagenumber".
            Pager.ModifyParam(pageNumber);

            return Pager.GenerateUrl(ActionContextAccessor.ActionContext);
        }
    }
}
