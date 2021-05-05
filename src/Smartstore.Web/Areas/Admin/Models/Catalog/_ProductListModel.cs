using System;
using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.List.")]
    public class ProductListModel : ModelBase
    {
        // TODO: (mh) (core) Finish Smartstore.Admin.Models.Catalog.ProductListModel

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

        [LocalizedDisplay("*GoDirectlyToSku")]
        public string GoDirectlyToSku { get; set; }

        public bool DisplayProductPictures { get; set; }
        public bool IsSingleStoreMode { get; set; }
        public int GridPageSize { get; set; }

        //public IList<SelectListItem> AvailableCategories { get; set; }
        //public IList<SelectListItem> AvailableManufacturers { get; set; }
        //public IList<SelectListItem> AvailableStores { get; set; }
        //public IList<SelectListItem> AvailableProductTypes { get; set; }
    }
}
