using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Customers
{
    public partial class CustomerReturnCasesModel : ModelBase
    {
        public List<CustomerReturnCaseModel> ReturnCases { get; set; } = [];
    }

    public partial class CustomerReturnCaseModel : EntityModelBase
    {
        public string ReturnCaseStatus { get; set; }
        public int ProductId { get; set; }
        public LocalizedValue<string> ProductName { get; set; }
        public string ProductSeName { get; set; }
        public string ProductUrl { get; set; }
        public int Quantity { get; set; }
        public int OrderItemId { get; set; }
        public DateTime CreatedOn { get; set; }

        public string ReturnReason { get; set; }
        public string ReturnAction { get; set; }
        public string Comments { get; set; }
    }
}
