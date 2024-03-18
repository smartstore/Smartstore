namespace Smartstore.Web.Models.Checkout
{
    public abstract partial class CheckoutModelBase : ModelBase
    {
        public List<string> Warnings { get; set; } = [];
        public string PreviousStepUrl { get; set; }
    }
}
