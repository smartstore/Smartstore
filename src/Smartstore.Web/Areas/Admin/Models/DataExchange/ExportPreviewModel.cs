using Smartstore.Core.DataExchange;

namespace Smartstore.Admin.Models.Export
{
    public class ExportPreviewModel : EntityModelBase
    {
        public string Name { get; set; }
        public ExportEntityType EntityType { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool LogFileExists { get; set; }
        public bool UsernamesEnabled { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class ExportPreviewProductModel : EntityModelBase
    {
        [LocalizedDisplay("*ProductType")]
        public int ProductTypeId { get; set; }
        [LocalizedDisplay("*ProductType")]
        public string ProductTypeName { get; set; }
        public string ProductTypeLabelHint { get; set; }
        public string EditUrl { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Sku")]
        public string Sku { get; set; }

        [LocalizedDisplay("*Price")]
        public decimal Price { get; set; }

        [LocalizedDisplay("*StockQuantity")]
        public int StockQuantity { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("*AdminComment")]
        public string AdminComment { get; set; }
    }

    [LocalizedDisplay("Admin.Orders.Fields.")]
    public class ExportPreviewOrderModel : EntityModelBase
    {
        public bool HasNewPaymentNotification { get; set; }
        public string EditUrl { get; set; }

        [LocalizedDisplay("*OrderNumber")]
        public string OrderNumber { get; set; }

        [LocalizedDisplay("*OrderStatus")]
        public string OrderStatus { get; set; }

        [LocalizedDisplay("*PaymentStatus")]
        public string PaymentStatus { get; set; }

        [LocalizedDisplay("*ShippingStatus")]
        public string ShippingStatus { get; set; }

        [LocalizedDisplay("Common.CustomerId")]
        public int CustomerId { get; set; }

        [LocalizedDisplay("*OrderTotal")]
        public decimal OrderTotal { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("*Store")]
        public string StoreName { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.Categories.Fields.")]
    public class ExportPreviewCategoryModel : EntityModelBase
    {
        [LocalizedDisplay("Admin.Catalog.Products.Categories.Fields.Category")]
        public string Breadcrumb { get; set; }

        [LocalizedDisplay("*FullName")]
        public string FullName { get; set; }

        [LocalizedDisplay("*Alias")]
        public string Alias { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.Manufacturers.Fields.")]
    public class ExportPreviewManufacturerModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }
    }

    [LocalizedDisplay("Admin.Customers.Customers.Fields.")]
    public class ExportPreviewCustomerModel : EntityModelBase
    {
        public bool UsernamesEnabled { get; set; }

        [LocalizedDisplay("*Username")]
        public string Username { get; set; }

        [LocalizedDisplay("*FullName")]
        public string FullName { get; set; }

        [LocalizedDisplay("*Email")]
        public string Email { get; set; }

        [LocalizedDisplay("*CustomerRoles")]
        public string CustomerRoleNames { get; set; }

        [LocalizedDisplay("*Active")]
        public bool Active { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("*LastActivityDate")]
        public DateTime LastActivityDate { get; set; }
    }

    [LocalizedDisplay("Admin.Promotions.NewsletterSubscriptions.Fields.")]
    public class ExportPreviewNewsletterSubscriptionModel : EntityModelBase
    {
        [LocalizedDisplay("*Email")]
        public string Email { get; set; }

        [LocalizedDisplay("*Active")]
        public bool Active { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("Admin.Common.Store")]
        public string StoreName { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class ExportPreviewShoppingCartItemModel : ExportPreviewProductModel
    {
        [LocalizedDisplay("Admin.DataExchange.Export.Filter.ShoppingCartTypeId")]
        public int ShoppingCartTypeId { get; set; }
        [LocalizedDisplay("Admin.DataExchange.Export.Filter.ShoppingCartTypeId")]
        public string ShoppingCartTypeName { get; set; }

        [LocalizedDisplay("Common.CustomerId")]
        public int CustomerId { get; set; }

        [LocalizedDisplay("Admin.CurrentCarts.Customer")]
        public string CustomerEmail { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("Admin.Common.Store")]
        public string StoreName { get; set; }
    }
}
