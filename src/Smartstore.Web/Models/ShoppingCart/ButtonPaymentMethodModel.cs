namespace Smartstore.Web.Models.Cart
{
    public partial class ButtonPaymentMethodModel : ModelBase
    {
        public List<WidgetInvoker> Items { get; set; } = new();
    }
}