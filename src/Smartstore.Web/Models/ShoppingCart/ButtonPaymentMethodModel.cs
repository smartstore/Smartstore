using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.ShoppingCart
{
    public partial class ButtonPaymentMethodModel : ModelBase
    {
        public List<ButtonPaymentMethodItem> Items { get; set; } = new();

        public partial class ButtonPaymentMethodItem
        {
            public string ActionName { get; set; }
            public string ControllerName { get; set; }
            public RouteValueDictionary RouteValues { get; set; }
        }
    }
}