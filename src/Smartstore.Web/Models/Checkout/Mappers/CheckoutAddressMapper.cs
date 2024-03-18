using System.Dynamic;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Models.Checkout
{
    public static partial class CheckoutAddressMappingExtensions
    {
        public static async Task<CheckoutAddressModel> MapAddressesAsync(this CheckoutContext context,
            bool shipping,
            int? selectedCountryId = null)
        {
            var model = new CheckoutAddressModel();
            await context.MapAddressesAsync(model, shipping, selectedCountryId);

            return model;
        }

        public static async Task MapAddressesAsync(this CheckoutContext context,
            CheckoutAddressModel model,
            bool shipping,
            int? selectedCountryId)
        {
            dynamic parameters = new ExpandoObject();
            parameters.SelectedCountryId = selectedCountryId;
            parameters.Shipping = shipping;

            await MapperFactory.MapAsync(context, model, parameters);
        }
    }

    public class CheckoutAddressMapper : Mapper<CheckoutContext, CheckoutAddressModel>
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICheckoutFactory _checkoutFactory;

        public CheckoutAddressMapper(
            SmartDbContext db,
            IStoreContext storeContext,
            IWorkContext workContext,
            IShoppingCartService shoppingCartService,
            ICheckoutFactory checkoutFactory)
        {
            _db = db;
            _storeContext = storeContext;
            _workContext = workContext;
            _shoppingCartService = shoppingCartService;
            _checkoutFactory = checkoutFactory;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override void Map(CheckoutContext from, CheckoutAddressModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(CheckoutContext from, CheckoutAddressModel to, dynamic parameters = null)
        {
            Guard.NotNull(to);

            var shipping = parameters?.Shipping == true;
            var selectedCountryId = parameters?.SelectedCountryId as int?;
            var cart = from.Cart;
            var addresses = cart.Customer.Addresses.Where(x => x.Country == null || (shipping ? x.Country.AllowsShipping : x.Country.AllowsBilling));

            foreach (var address in addresses)
            {
                to.ExistingAddresses.Add(await address.MapAsync());
            }

            to.IsShippingRequired = cart.IsShippingRequired;
            to.PreviousStepUrl = _checkoutFactory.GetNextCheckoutStepUrl(from, false);

            // New address.
            await new Address().MapAsync(to.NewAddress);

            to.NewAddress.CountryId = selectedCountryId;
            to.NewAddress.Email = _workContext.CurrentCustomer.Email;

            if (to.NewAddress.CountryEnabled)
            {
                var countriesQuery = _db.Countries.AsNoTracking();

                countriesQuery = shipping
                    ? countriesQuery.Where(x => x.AllowsShipping)
                    : countriesQuery.Where(x => x.AllowsBilling);

                var countries = await countriesQuery
                    .ApplyStandardFilter(false, _storeContext.CurrentStore.Id)
                    .ToListAsync();

                to.NewAddress.AvailableCountries = countries.ToSelectListItems(selectedCountryId ?? 0);
            }
        }
    }
}
