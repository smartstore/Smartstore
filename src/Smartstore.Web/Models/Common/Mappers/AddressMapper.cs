using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Common
{
    public static class AddressMappingExtensions
    {
        /// <summary>
        /// Maps an <see cref="Address"/> entity to <see cref="AddressModel"/>.
        /// </summary>
        /// <param name="entity">Source <see cref="Address"/> to be mapped.</param>
        /// <param name="model">Target <see cref="AddressModel"/> to which <paramref name="entity"/> is to be mapped.</param>
        public static async Task MapAsync(this Address entity, AddressModel model)
        {
            await MapperFactory.MapAsync(entity, model);
        }

        /// <summary>
        /// Creates a new <see cref="AddressModel"/> and maps an <see cref="Address"/> entity to it.
        /// </summary>
        /// <param name="entity">Source <see cref="Address"/> to be mapped.</param>
        /// <returns>New <see cref="AddressModel"/>.</returns>
        public static async Task<AddressModel> MapAsync(this Address entity)
        {
            var model = new AddressModel();
            await MapperFactory.MapAsync(entity, model);

            return model;
        }
    }

    internal class AddressMapper : Mapper<Address, AddressModel>
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

        protected override void Map(Address from, AddressModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(Address from, AddressModel to, dynamic parameters = null)
        {
            Guard.NotNull(to, nameof(to));

            MiniMapper.Map(_addressSettings, to);

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

            string salutations = _addressSettings.GetLocalizedSetting(x => x.Salutations);

            foreach (var salutation in salutations.SplitSafe(','))
            {
                to.AvailableSalutations.Add(new SelectListItem { Text = salutation, Value = salutation });
            }
        }
    }
}
