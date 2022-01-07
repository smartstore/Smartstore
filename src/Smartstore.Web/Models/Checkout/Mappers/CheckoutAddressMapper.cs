using System.Dynamic;
using Smartstore.ComponentModel;
using Smartstore.Web.Models.Checkout;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Models.Checkout
{
    public static partial class CheckoutAddressMappingExtensions
    {
        public static async Task MapAsync(this IEnumerable<Address> entity,
            CheckoutAddressModel model,
            bool shipping,
            int? selectedCountryId)
        {
            dynamic parameters = new ExpandoObject();
            parameters.SelectedCountryId = selectedCountryId;            
            parameters.Shipping = shipping;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    public class CheckoutAddressMapper : Mapper<IEnumerable<Address>, CheckoutAddressModel>
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;

        public CheckoutAddressMapper(SmartDbContext db,
            ICommonServices services)
        {
            _db = db;
            _services = services;
        }

        protected override void Map(IEnumerable<Address> from, CheckoutAddressModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(IEnumerable<Address> from, CheckoutAddressModel to, dynamic parameters = null)
        {
            Guard.NotNull(to, nameof(to));

            var shipping = parameters?.Shipping == true;
            var selectedCountryId = parameters?.SelectedCountryId as int?;

            from = shipping
                ? from.Where(x => x.Country == null || x.Country.AllowsShipping)
                : from.Where(x => x.Country == null || x.Country.AllowsBilling);

            foreach (var address in from)
            {
                var addressModel = new AddressModel();
                await address.MapAsync(addressModel, false);

                to.ExistingAddresses.Add(addressModel);
            }

            // New address.
            var countriesQuery = _db.Countries.AsNoTracking();

            countriesQuery = shipping
                ? countriesQuery.Where(x => x.AllowsShipping)
                : countriesQuery.Where(x => x.AllowsBilling);

            var countries = await countriesQuery
                .ApplyStandardFilter(false, _services.StoreContext.CurrentStore.Id)
                .ToListAsync();

            await new Address().MapAsync(to.NewAddress, null, countries);

            to.NewAddress.CountryId = selectedCountryId;
            to.NewAddress.Email = _services.WorkContext.CurrentCustomer.Email;
        }
    }
}
