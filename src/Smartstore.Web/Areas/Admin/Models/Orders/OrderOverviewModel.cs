using System;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.Fields.")]
    public class OrderOverviewModel : TabbableModel
    {
        [LocalizedDisplay("*ID")]
        public override int Id { get; set; }

        [LocalizedDisplay("*OrderNumber")]
        public string OrderNumber { get; set; }

        [LocalizedDisplay("*OrderGuid")]
        public Guid OrderGuid { get; set; }

        [LocalizedDisplay("*Store")]
        public string StoreName { get; set; }
        public string FromStore { get; set; }

        [LocalizedDisplay("*Customer")]
        public int CustomerId { get; set; }

        [LocalizedDisplay("Admin.Orders.List.CustomerName")]
        public string CustomerName { get; set; }

        [LocalizedDisplay("*CustomerEmail")]
        public string CustomerEmail { get; set; }

        //...
    }
}
