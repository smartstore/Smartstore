using System.Linq;
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
    // TODO: (mh) (core) Will not be registered automatically and thus not be used for mapping yet.
    public class AddressMapper : IMapper<Address, AddressModel>
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

        public void Map(Address from, AddressModel to)
        {
            // INFO & TODO: (mh) (core) Legacy params. Was false most of the time.
            var excludeProperties = false;

            // INFO: (mh) (core) no async :-/
            var loadCountries = _db.Countries
                    .AsNoTracking()
                    .ApplyStandardFilter()
                    .ToList();

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
            if (_addressSettings.CountryEnabled && loadCountries != null)
            {
                to.AvailableCountries.Add(new SelectListItem { Text = _services.Localization.GetResource("Address.SelectCountry"), Value = "0" });
                foreach (var c in loadCountries)
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
                    var states = _db.StateProvinces.Where(x => x.CountryId == (to.CountryId ?? 0)).ToList();

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
