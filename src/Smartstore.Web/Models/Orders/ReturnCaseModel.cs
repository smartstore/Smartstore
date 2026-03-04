using Smartstore.Core.Localization;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Orders
{
    [LocalizedDisplay("ReturnRequests.")]
    public partial class ReturnCaseModel : ModelBase
    {
        public int OrderId { get; set; }

        public ReturnCaseItemsModel Items { get; set; }

        [LocalizedDisplay("*ReturnReason")]
        public string ReturnReason { get; set; }

        [LocalizedDisplay("*ReturnAction")]
        public string ReturnAction { get; set; }

        [SanitizeHtml]
        [LocalizedDisplay("*Comments")]
        public string Comments { get; set; }

        public bool HasItemsToReturn
            => Items?.Items?.Any(x => x.MaxReturnQuantity > 0) ?? false;
    }

    public partial class ReturnCaseItemsModel : ModelBase
    {
        public bool IsEditable { get; set; } = true;
        public bool ReturnAllItems { get; set; } = true;

        public List<ItemModel> Items { get; set; } = [];

        public partial class ItemModel : EntityModelBase
        {
            public int ProductId { get; set; }
            public LocalizedValue<string> ProductName { get; set; }
            public string ProductSeName { get; set; }
            public string ProductUrl { get; set; }
            public string AttributeInfo { get; set; }
            public Money UnitPrice { get; set; }
            public int Quantity { get; set; }
            public string QuantityUnit { get; set; }

            public ImageModel Image { get; set; }

            public bool Selected { get; set; }
            public int SelectedReturnQuantity { get; set; }

            public int MaxReturnQuantity { get; set; }

            public List<CustomerReturnCaseModel> ReturnCases { get; set; }
        }
    }
}
