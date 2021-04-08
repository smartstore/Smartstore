using System;
using System.Collections.Generic;
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
        /// TODO: (mh) (core) Write docs. Especially parameters.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="parameters"></param>
        public override async Task MapAsync(Address from, AddressModel to, dynamic parameters = null)
        {
            var excludeProperties = false;
            var countries = new List<Country>();
            if (parameters != null)
            {
                var fastPropExcludeProperties = FastProperty.GetProperty(parameters.GetType(), "excludeProperties", PropertyCachingStrategy.Uncached);
                if (fastPropExcludeProperties != null)
                {
                    excludeProperties = fastPropExcludeProperties.GetValue(parameters);
                }

                var fastPropCountries = FastProperty.GetProperty(parameters.GetType(), "countries", PropertyCachingStrategy.Uncached);
                if (fastPropCountries != null)
                {
                    countries = fastPropCountries.GetValue(parameters);
                }
            }
            
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
            if (_addressSettings.CountryEnabled && countries.Count > 0)
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

                    if (states.Count > 0)
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
            foreach (var sal in salutations.SplitSafe(","))
            {
                to.AvailableSalutations.Add(new SelectListItem { Value = sal, Text = sal });
            }            
        }
    }
}
