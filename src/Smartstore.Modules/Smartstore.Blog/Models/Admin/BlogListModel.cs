using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Blog.Models
{
    [LocalizedDisplay("Admin.ContentManagement.Blog.BlogPosts.Fields.")]
    public class BlogListModel
    {
        [LocalizedDisplay("*Title")]
        public string SearchTitle { get; set; }

        [LocalizedDisplay("*Intro")]
        public string SearchIntro { get; set; }

        [LocalizedDisplay("*Body")]
        public string SearchBody { get; set; }

        [LocalizedDisplay("*Tags")]
        public string SearchTags { get; set; }
        public MultiSelectList SearchAvailableTags { get; set; }

        [LocalizedDisplay("*StartDate")]
        public DateTime? SearchStartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? SearchEndDate { get; set; }

        [UIHint("Stores")]
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }

        [LocalizedDisplay("Admin.Common.IsPublished")]
        public bool? SearchIsPublished { get; set; }

        [LocalizedDisplay("*Language")]
        public int SearchLanguageId { get; set; }
    }
}
