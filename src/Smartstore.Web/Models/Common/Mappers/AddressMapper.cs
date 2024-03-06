using System.Dynamic;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Common
{
    public static class AddressMappingExtensions
    {
        public static async Task<List<AddressModel>> MapAsync(this IEnumerable<Address> entities,
            Customer customer = null,
            bool defaultAddressesEnabled = false)
        {
            var models = entities.SelectAwait(async x => await x.MapAsync(customer, defaultAddressesEnabled));

            if (defaultAddressesEnabled)
            {
                return await models.OrderByDescending(x =>
                {
                    if (x.IsDefaultBillingAddress && x.IsDefaultShippingAddress)
                        return int.MaxValue;
                    else if (x.IsDefaultBillingAddress)
                        return int.MaxValue - 1;
                    else if (x.IsDefaultShippingAddress)
                        return int.MaxValue - 2;

                    return x.Id;
                })
                .AsyncToList();
            }

            return await models.OrderByDescending(x => x.Id).AsyncToList();
        }

        /// <summary>
        /// Creates a new <see cref="AddressModel"/> and maps an <see cref="Address"/> entity to it.
        /// </summary>
        /// <param name="entity">Source <see cref="Address"/> to be mapped.</param>
        /// <returns>New <see cref="AddressModel"/>.</returns>
        public static async Task<AddressModel> MapAsync(this Address entity, 
            Customer customer = null,
            bool defaultAddressesEnabled = false)
        {
            var model = new AddressModel();
            await MapAsync(entity, model, customer, defaultAddressesEnabled);

            return model;
        }

        /// <summary>
        /// Maps an <see cref="Address"/> entity to <see cref="AddressModel"/>.
        /// </summary>
        /// <param name="entity">Source <see cref="Address"/> to be mapped.</param>
        /// <param name="model">Target <see cref="AddressModel"/> to which <paramref name="entity"/> is to be mapped.</param>
        public static async Task MapAsync(this Address entity, 
            AddressModel model, 
            Customer customer = null,
            bool defaultAddressesEnabled = false)
        {
            dynamic parameters = new ExpandoObject();
            parameters.Customer = customer;
            parameters.DefaultAddressesEnabled = defaultAddressesEnabled;

            await MapperFactory.MapAsync(entity, model, parameters);
        }


        public static async Task MapAsync(this AddressModel model,
            Address entity,
            Customer customer = null,
            bool applyDefaultAddresses = false)
        {
            dynamic parameters = new ExpandoObject();
            parameters.Customer = customer;
            parameters.ApplyDefaultAddresses = applyDefaultAddresses;

            await MapperFactory.MapAsync(model, entity, parameters);
        }
    }

    internal class AddressMapper : 
        IMapper<Address, AddressModel>,
        IMapper<AddressModel, Address>
    {
        private readonly SmartDbContext _db;
        private readonly IAddressService _addressService;
        private readonly AddressSettings _addressSettings;

        public AddressMapper(
            SmartDbContext db,
            IAddressService addressService,
            AddressSettings addressSettings)
        {
            _db = db;
            _addressService = addressService;
            _addressSettings = addressSettings;
        }

        public async Task MapAsync(Address from, AddressModel to, dynamic parameters = null)
        {
            Guard.NotNull(to);

            var customer = parameters?.Customer as Customer;

            MiniMapper.Map(_addressSettings, to);

            to.DefaultAddressesEnabled = parameters?.DefaultAddressesEnabled == true;

            // INFO: transient entity is not mapped to re-display entered model values when model validation failed.
            if (from != null && !from.IsTransientRecord())
            {
                await _db.LoadReferenceAsync(from, x => x.Country);
                await _db.LoadReferenceAsync(from, x => x.StateProvince);

                MiniMapper.Map(from, to);

                to.EmailMatch = from.Email;
                to.CountryName = from.Country?.GetLocalized(x => x.Name);
                to.StateProvinceName = from.StateProvince?.GetLocalized(x => x.Name);
                to.FormattedAddress = await _addressService.FormatAddressAsync(from, true);
            }

            if (customer != null)
            {
                to.IsDefaultBillingAddress = from.Id == customer.GenericAttributes.DefaultBillingAddressId;
                to.IsDefaultShippingAddress = from.Id == customer.GenericAttributes.DefaultShippingAddressId;
            }

            var salutations = _addressSettings.GetLocalizedSetting(x => x.Salutations).Value.SplitSafe(',');
            foreach (var salutation in salutations)
            {
                to.AvailableSalutations.Add(new() { Text = salutation, Value = salutation });
            }
        }

        public Task MapAsync(AddressModel from, Address to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            MiniMapper.Map(from, to);

            var customer = parameters?.Customer as Customer;
            var applyDefaultAddresses = parameters?.ApplyDefaultAddresses == true;

            if (applyDefaultAddresses && customer != null && from.Id != 0)
            {
                var ga = customer.GenericAttributes;

                if (ga.DefaultBillingAddressId == from.Id && !from.IsDefaultBillingAddress)
                {
                    ga.DefaultBillingAddressId = null;
                }
                else if (ga.DefaultBillingAddressId != from.Id && from.IsDefaultBillingAddress)
                {
                    ga.DefaultBillingAddressId = from.Id;
                }

                if (ga.DefaultShippingAddressId == from.Id && !from.IsDefaultShippingAddress)
                {
                    ga.DefaultShippingAddressId = null;
                }
                else if (ga.DefaultShippingAddressId != from.Id && from.IsDefaultShippingAddress)
                {
                    ga.DefaultShippingAddressId = from.Id;
                }
            }

            return Task.CompletedTask;
        }
    }
}
