using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Common
{
    public static class AddressMappingExtensions
    {
        /// <summary>
        /// Extension method to map an <see cref="Address"/> entity to the corresponding <see cref="AddressModel"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Address"/> entity that should be mapped.</param>
        /// <param name="model">The <see cref="AddressModel"/> to which the entity should be mapped to.</param>
        /// <param name="excludeProperties">Specifies whether to exclude entity properties from being mapped to the model.</param>
        /// <param name="countries">List of countries which should be included in the model.</param>
        public static async Task MapAsync(this Address entity, AddressModel model, bool excludeProperties = false, List<Country> countries = null)
        {
            dynamic parameters = new ExpandoObject();
            parameters.ExcludeProperties = excludeProperties;
            parameters.Countries = countries;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    internal class AddressMapper : Mapper<Address, AddressModel>
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IAddressService _addressService;
        private readonly AddressSettings _addressSettings;

        public AddressMapper(
            SmartDbContext db, 
            ICommonServices services, 
            IAddressService addressService, 
            AddressSettings addressSettings)
        {
            _db = db;
            _services = services;
            _addressService = addressService;
            _addressSettings = addressSettings;
        }

        protected override void Map(Address from, AddressModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        /// <summary>
        /// Maps an address entity to an address model.
        /// </summary>
        /// <param name="from"><see cref="Address"/></param>
        /// <param name="to"><see cref="AddressModel"/></param>
        /// <param name="parameters">Expects excludeProperties of type <see cref="bool"/> and countries of type <see cref="List<Country>"/>. Both properties can also be ommited.</param>
        public override async Task MapAsync(Address from, AddressModel to, dynamic parameters = null)
        {
            var excludeProperties = parameters?.ExcludeProperties == true;
            var countries = parameters?.Countries as IEnumerable<Country>;
            
            // Form fields
            MiniMapper.Map(_addressSettings, to);

            if (!excludeProperties && from != null)
            {
                MiniMapper.Map(from, to);

                to.EmailMatch = from.Email;
                to.CountryName = from.Country?.GetLocalized(x => x.Name);
                if (from.StateProvinceId.HasValue && from.StateProvince != null)
                {
                    to.StateProvinceName = from.StateProvince.GetLocalized(x => x.Name);
                }
                
                to.FormattedAddress = await _addressService.FormatAddressAsync(from, true);
            }

            // Countries and states
            if (_addressSettings.CountryEnabled && countries != null && countries.Any())
            {
                to.AvailableCountries.Add(new SelectListItem { Text = _services.Localization.GetResource("Address.SelectCountry"), Value = "0" });
                foreach (var c in countries)
                {
                    to.AvailableCountries.Add(new SelectListItem
                    {
                        Text = c.GetLocalized(x => x.Name),
                        Value = c.Id.ToString(),
                        Selected = c.Id == to.CountryId
                    });
                }

                if (_addressSettings.StateProvinceEnabled)
                {
                    var states = await _db.StateProvinces
                        .AsNoTracking()
                        .Where(x => x.CountryId == (to.CountryId ?? 0))
                        .ToListAsync();

                    if (states.Any())
                    {
                        foreach (var s in states)
                        {
                            to.AvailableStates.Add(new SelectListItem
                            {
                                Text = s.GetLocalized(x => x.Name),
                                Value = s.Id.ToString(),
                                Selected = (s.Id == to.StateProvinceId)
                            });
                        }
                    }
                    else
                    {
                        to.AvailableStates.Add(new SelectListItem
                        {
                            Text = _services.Localization.GetResource("Address.OtherNonUS"),
                            Value = "0"
                        });
                    }
                }
            }

            string salutations = _addressSettings.GetLocalizedSetting(x => x.Salutations);
            foreach (var sal in salutations.SplitSafe(','))
            {
                to.AvailableSalutations.Add(new SelectListItem { Value = sal, Text = sal });
            }            
        }   
    }
}
