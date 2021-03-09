using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Web.Rendering.Pager;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("div")]
    [HtmlTargetElement("forum-pagination", TagStructure = TagStructure.WithoutEndTag)]
    public class ForumPaginationTagHelper : PaginationTagHelper
    {
        public ForumPaginationTagHelper() 
            : base()
        {
        }

        protected override void AddPageItemsList(List<PagerItem> items)
        {
            var maxPages = MaxPagesToDisplay;
            var pageCount = ListItems.TotalPages;

            int start = 1;
            if (pageCount > maxPages + 1)
            {
                start = (pageCount + 1) - maxPages;
            }

            int end = ListItems.TotalPages;
            if (start > 2)
            {
                items.Add(new PagerItem("1", GenerateUrl(1)));
                items.Add(new PagerItem("...", "", PagerItemType.Text));
            }

            for (int i = start; i <= end; i++)
            {
                var item = new PagerItem(i.ToString(), GenerateUrl(i));
                items.Add(item);
            }
        }
    }
}
