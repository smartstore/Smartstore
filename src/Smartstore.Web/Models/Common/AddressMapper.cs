using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Common
{
    internal class AddressMapper : Mapper<Address, AddressModel>
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly AddressSettings _addressSettings;

        public AddressMapper(SmartDbContext db, ICommonServices services, AddressSettings addressSettings)
        {
            _db = db;
            _services = services;
            _addressSettings = addressSettings;
        }

        protected override void Map(Address from, AddressModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(Address from, AddressModel to, dynamic parameters = null)
        {
            // INFO & TODO: (mh) (core) Legacy params. Was false most of the time.
            var excludeProperties = false;

            // TODO: (mh) (core) This mapper does not behave like original one. In classic, a countries loader delegate
            // is passed to the mapping function, whereas here ALL countries are loaded (ALWAYS!).

            var countries = await _db.Countries
                .AsNoTracking()
                .ApplyStandardFilter()
                .ToListAsync();

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
                // TODO: (mh) (core) Implement IAddressService & Inject.
                //to.FormattedAddress = Core.Infrastructure.EngineContext.Current.Resolve<IAddressService>().FormatAddress(address, true);
            }

            // Countries and states
            if (_addressSettings.CountryEnabled && countries != null)
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
