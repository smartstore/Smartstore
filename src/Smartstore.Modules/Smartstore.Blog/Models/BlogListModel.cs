using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Web.Modelling;

namespace Smartstore.Blog.Models
{
    [LocalizedDisplay("Admin.ContentManagement.Blog.BlogPosts.Fields.")]
    public class BlogListModel : TabbableModel
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
        //public List<SelectListItem> AvailableLanguages { get; set; }

        public bool IsSingleStoreMode { get; set; }
        public bool IsSingleLanguageMode { get; set; }
        public int GridPageSize { get; set; }
    }
}
