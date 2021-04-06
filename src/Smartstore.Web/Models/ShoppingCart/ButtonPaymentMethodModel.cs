using System.Collections.Generic;
using Smartstore.Core.Widgets;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.ShoppingCart
{
    public partial class ButtonPaymentMethodModel : ModelBase
    {
        public List<WidgetInvoker> Items { get; set; } = new();
    }
}