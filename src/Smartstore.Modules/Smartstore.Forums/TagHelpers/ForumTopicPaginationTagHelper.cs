using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering.Pager;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Forums.TagHelpers
{
    [OutputElementHint("div")]
    [HtmlTargetElement("forumtopic-pagination", TagStructure = TagStructure.WithoutEndTag)]
    public class ForumTopicPaginationTagHelper : PaginationTagHelper
    {
        const string TopicIdAttributeName = "sm-topic-id";
        const string TopicSlugAttributeName = "sm-topic-slug";

        [HtmlAttributeName(TopicIdAttributeName)]
        public int TopicId { get; set; }

        [HtmlAttributeName(TopicSlugAttributeName)]
        public string TopicSlug { get; set; }

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

        protected override string GenerateUrl(int pageNumber)
        {
            return UrlHelper.RouteUrl("ForumTopicBySlugPaged", new { id = TopicId, slug = TopicSlug, page = pageNumber });
        }
    }
}
