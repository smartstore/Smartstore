using Smartstore.Core.Checkout.Orders;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.ReturnRequests.Fields.")]
    public class ReturnRequestListModel : ModelBase
    {
        [LocalizedDisplay("*ID")]
        public int? SearchId { get; set; }

        [LocalizedDisplay("*Status")]
        public int? SearchReturnRequestStatusId { get; set; }

        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }

        public ReturnRequestStatus? SearchReturnRequestStatus
            => SearchReturnRequestStatusId.HasValue ? (ReturnRequestStatus)SearchReturnRequestStatusId.Value : null;
    }
}
