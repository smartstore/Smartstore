using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Web.Models.Cart
{
    [LocalizedDisplay("ShoppingCart.EstimateShipping.")]
    public partial class EstimateShippingModel : ModelBase
    {
        public bool Enabled { get; set; }
        public string ShippingInfoUrl { get; set; }
        public List<ShippingOptionModel> ShippingOptions { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        [LocalizedDisplay("*Country")]
        public int? CountryId { get; set; }

        [LocalizedDisplay("*StateProvince")]
        public int? StateProvinceId { get; set; }

        [LocalizedDisplay("*ZipPostalCode")]
        public string ZipPostalCode { get; set; }

        public IList<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();
        public IList<SelectListItem> AvailableStates { get; set; } = new List<SelectListItem>();

        public partial class ShippingOptionModel : ModelBase
        {
            public int ShippingMethodId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Price { get; set; }
        }
    }
}