using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Categories.List.")]
    public class CategoryListModel : ModelBase
    {
        [LocalizedDisplay("*SearchCategoryName")]
        public string SearchCategoryName { get; set; }

        [LocalizedDisplay("*SearchAlias")]
        public string SearchAlias { get; set; }

        [UIHint("Stores")]
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }
    }
}
