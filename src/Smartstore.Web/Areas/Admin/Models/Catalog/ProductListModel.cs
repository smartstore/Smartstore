using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.List.")]
    public class ProductListModel : ModelBase
    {
        public GridModel<ProductModel> Products { get; set; }

        [LocalizedDisplay("*SearchProductName")]
        public string SearchProductName { get; set; }

        [LocalizedDisplay("*SearchCategory")]
        public int SearchCategoryId { get; set; }

        [LocalizedDisplay("*SearchWithoutCategories")]
        public bool? SearchWithoutCategories { get; set; }

        [LocalizedDisplay("*SearchManufacturer")]
        public int SearchManufacturerId { get; set; }

        [LocalizedDisplay("*SearchWithoutManufacturers")]
        public bool? SearchWithoutManufacturers { get; set; }

        [UIHint("Stores")]
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }

        [LocalizedDisplay("*SearchProductType")]
        public int SearchProductTypeId { get; set; }

        [LocalizedDisplay("*SearchIsPublished")]
        public bool? SearchIsPublished { get; set; }

        [LocalizedDisplay("*SearchHomePageProducts")]
        public bool? SearchHomePageProducts { get; set; }

        [LocalizedDisplay("*SearchDeliveryTime")]
        public int[] SearchDeliveryTimeIds { get; set; }

        [LocalizedDisplay("*GoDirectlyToSku")]
        public string GoDirectlyToSku { get; set; }

        public bool DisplayProductPictures { get; set; }
        public bool IsSingleStoreMode { get; set; }

        public List<SelectListItem> AvailableCategories { get; set; } = new();
        public List<SelectListItem> AvailableManufacturers { get; set; } = new();
        public List<SelectListItem> AvailableStores { get; set; } = new();
        public List<SelectListItem> AvailableProductTypes { get; set; } = new();
    }
}
