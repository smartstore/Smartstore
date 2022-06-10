using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Models.Common;

namespace Smartstore.Admin.Models.Affiliates
{
    [LocalizedDisplay("Admin.Affiliates.Fields.")]
    public class AffiliateModel : EntityModelBase
    {
        [LocalizedDisplay("*ID")]
        public override int Id { get; set; }

        [LocalizedDisplay("*URL")]
        public string Url { get; set; }

        [LocalizedDisplay("*Active")]
        public bool Active { get; set; }

        [UIHint("Address")]
        public AddressModel Address { get; set; } = new();

        public string EditUrl { get; set; }
        public bool UsernamesEnabled { get; set; }

        [LocalizedDisplay("Admin.Affiliates.Orders.")]
        public class AffiliatedOrderModel : EntityModelBase
        {
            [LocalizedDisplay("*Order")]
            public override int Id { get; set; }

            [LocalizedDisplay("*OrderStatus")]
            public string OrderStatus { get; set; }

            [LocalizedDisplay("*PaymentStatus")]
            public string PaymentStatus { get; set; }

            [LocalizedDisplay("*ShippingStatus")]
            public string ShippingStatus { get; set; }

            [LocalizedDisplay("*OrderTotal")]
            public Money OrderTotal { get; set; }

            [LocalizedDisplay("Common.CreatedOn")]
            public DateTime CreatedOn { get; set; }

            public string EditUrl { get; set; }
        }

        [LocalizedDisplay("Admin.Customers.Customers.Fields.")]
        public class AffiliatedCustomerModel : EntityModelBase
        {
            [LocalizedDisplay("*Email")]
            public string Email { get; set; }

            [LocalizedDisplay("*Username")]
            public string Username { get; set; }

            [LocalizedDisplay("*FullName")]
            public string FullName { get; set; }

            [LocalizedDisplay("Common.CreatedOn")]
            public DateTime CreatedOn { get; set; }

            public string EditUrl { get; set; }
        }
    }
}
