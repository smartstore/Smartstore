using Smartstore.Core.Common;
using Smartstore.Web.Modelling;
using System;

namespace Smartstore.Admin.Models.ShoppingCart
{
    [LocalizedDisplay("Admin.CurrentCarts.")]
    public class ShoppingCartItemModel : EntityModelBase
    {
        [LocalizedDisplay("Admin.Common.Store")]
        public string Store { get; set; }

        [LocalizedDisplay("*Product")]
        public int ProductId { get; set; }

        [LocalizedDisplay("*Product")]
        public string ProductName { get; set; }
        public string ProductTypeName { get; set; }
        public string ProductTypeLabelHint { get; set; }

        [LocalizedDisplay("*UnitPrice")]
        public Money UnitPrice { get; set; }

        [LocalizedDisplay("*Quantity")]
        public int Quantity { get; set; }

        [LocalizedDisplay("*Total")]
        public Money Total { get; set; }

        [LocalizedDisplay("*UpdatedOn")]
        public DateTime UpdatedOn { get; set; }
    }
}
