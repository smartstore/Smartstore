namespace Smartstore.Web.Models.Checkout
{
    public partial class CheckoutShippingMethodModel : CheckoutModelBase
    {
        public List<ShippingMethodModel> ShippingMethods { get; set; } = [];

        public partial class ShippingMethodModel : ModelBase
        {
            public int ShippingMethodId { get; set; }
            public string ShippingRateComputationMethodSystemName { get; set; }
            public string Name { get; set; }
            public string BrandUrl { get; set; }
            public string Description { get; set; }
            public Money Fee { get; set; }
            public bool Selected { get; set; }
        }
    }
}