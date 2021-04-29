using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Web.Models.Checkout;
using Smartstore.Web.Models.Common;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

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
                await address.MapAsync(addressModel);
                to.ExistingAddresses.Add(addressModel);
            }

            // New address.
            to.NewAddress.CountryId = selectedCountryId;

            var query = _db.Countries
                .AsNoTracking()
                .ApplyStandardFilter(false, _services.StoreContext.CurrentStore.Id)                
                .AsQueryable();

            query = shipping
                ? query.Where(x => x.AllowsShipping)
                : query.Where(x => x.AllowsBilling);

            var countries = await query.ToListAsync();

            await new Address().MapAsync(to.NewAddress, true, countries);

            to.NewAddress.Email = _services.WorkContext.CurrentCustomer.Email;
        }
    }
}
