namespace Smartstore.News.Models
{
    [LocalizedDisplay("Admin.ContentManagement.News.NewsItems.Fields.")]
    public partial class NewsListModel : ModelBase
    {
        [LocalizedDisplay("*Title")]
        public string SearchTitle { get; set; }

        [LocalizedDisplay("*Short")]
        public string SearchShort { get; set; }

        [LocalizedDisplay("*Full")]
        public string SearchFull { get; set; }

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
