using Smartstore.Web.Modelling;

namespace Smartstore.Web.Rendering
{
    [LocalizedDisplay("Admin.Catalog.Products.List.")]
    public class EntityPickerModel : ModelBase
    {
        public string EntityType { get; set; }
        public bool HighlightSearchTerm { get; set; }
        public string DisableIf { get; set; }
        public string DisableIds { get; set; }
        public string SearchTerm { get; set; }
        public string ReturnField { get; set; }
        public int MaxItems { get; set; }
        public string Selected { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; } = 96;

        public List<SearchResultModel> SearchResult { get; set; }

        #region Search properties

        [LocalizedDisplay("*SearchProductName")]
        public string ProductName { get; set; }

        [LocalizedDisplay("*SearchCategory")]
        public int CategoryId { get; set; }

        [LocalizedDisplay("*SearchManufacturer")]
        public int ManufacturerId { get; set; }

        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int StoreId { get; set; }

        [LocalizedDisplay("*SearchProductType")]
        public int ProductTypeId { get; set; }

        [LocalizedDisplay("Admin.Catalog.Customers.CustomerSearchType")]
        public string CustomerSearchType { get; set; }

        #endregion

        public class SearchResultModel : EntityModelBase
        {
            public string ReturnValue { get; set; }
            public string Title { get; set; }
            public string Summary { get; set; }
            public string SummaryTitle { get; set; }
            public bool? Published { get; set; }
            public bool Disable { get; set; }
            public bool Selected { get; set; }
            public string ImageUrl { get; set; }
            public string LabelText { get; set; }
            public string LabelClassName { get; set; }
        }
    }

    public class EntityPickerProduct
    {
        public int Id { get; set; }
        public string Sku { get; set; }
        public string Name { get; set; }
        public bool Published { get; set; }
        public int ProductTypeId { get; set; }
        public int? MainPictureId { get; set; }
    }
}
