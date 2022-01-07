using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Orders
{
    [LocalizedDisplay("ReturnRequests.")]
    public partial class SubmitReturnRequestModel : ModelBase
    {
        public int OrderId { get; set; }
        public List<OrderItemModel> Items { get; set; } = new();
        public List<int> AddedReturnRequestIds { get; set; } = new();

        [LocalizedDisplay("*ReturnReason")]
        public string ReturnReason { get; set; }

        [LocalizedDisplay("*ReturnAction")]
        public string ReturnAction { get; set; }

        [SanitizeHtml]
        [LocalizedDisplay("*Comments")]
        public string Comments { get; set; }

        public string Result { get; set; }

        public partial class OrderItemModel : EntityModelBase
        {
            public int ProductId { get; set; }

            public LocalizedValue<string> ProductName { get; set; }

            public string ProductSeName { get; set; }

            public string ProductUrl { get; set; }

            public string AttributeInfo { get; set; }

            public Money UnitPrice { get; set; }

            public int Quantity { get; set; }
        }
    }
}
