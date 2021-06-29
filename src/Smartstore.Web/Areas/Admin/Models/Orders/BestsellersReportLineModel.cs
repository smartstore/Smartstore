using Smartstore.Core.Common;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.SalesReport.Bestsellers.Fields.")]
    public class BestsellersReportLineModel : ModelBase
    {
        [LocalizedDisplay("*TotalAmount")]
        public Money TotalAmount { get; set; }

        [LocalizedDisplay("*TotalQuantity")]
        public string TotalQuantity { get; set; }

        [LocalizedDisplay("Admin.Common.Entity.Fields.Id")]
        public int ProductId { get; set; }
        public string ProductTypeName { get; set; }
        public string ProductTypeLabelHint { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.PictureThumbnailUrl")]
        public string PictureThumbnailUrl { get; set; }
        public bool NoThumb { get; set; }

        [LocalizedDisplay("*Name")]
        public string ProductName { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.Sku")]
        public string Sku { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.StockQuantity")]
        public int StockQuantity { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.Price")]
        public decimal Price { get; set; }
    }
}
