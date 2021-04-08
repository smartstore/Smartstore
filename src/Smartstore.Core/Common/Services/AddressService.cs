using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Messages;
using Smartstore.Data.Hooks;
using Smartstore.Templating;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.Common.Services
{
    public partial class AddressService : AsyncDbSaveHook<Address>, IAddressService
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ITemplateEngine _templateEngine;
        private readonly IMessageModelProvider _messageModelProvider;
        private readonly AddressSettings _addressSettings;

        public AddressService(
            SmartDbContext db,
            ICommonServices services,
            ITemplateEngine templateEngine,
            IMessageModelProvider messageModelProvider,
            AddressSettings addressSettings)
        {
            _db = db;
            _services = services;
            _addressSettings = addressSettings;
            _templateEngine = templateEngine;
            _messageModelProvider = messageModelProvider;
        }

        #region Hook 

        protected override Task<HookResult> OnInsertingAsync(Address entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            entity.CreatedOnUtc = DateTime.UtcNow;

            if (entity.CountryId == 0)
            {
                entity.CountryId = null;
            }
                
            if (entity.StateProvinceId == 0)
            {
                entity.StateProvinceId = null;
            }
            
            return Task.FromResult(HookResult.Ok);
        }
    
        protected override Task<HookResult> OnUpdatingAsync(Address entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.CountryId == 0)
            {
                entity.CountryId = null;
            }

            if (entity.StateProvinceId == 0)
            {
                entity.StateProvinceId = null;
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnInsertedAsync(Address entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(Address entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        #endregion

        public virtual async Task<bool> IsAddressValidAsync(Address address)
        {
            Guard.NotNull(address, nameof(address));

            if (!address.FirstName.HasValue())
                return false;

            if (!address.LastName.HasValue())
                return false;

            if (!address.Email.HasValue())
                return false;

            if (_addressSettings.CompanyEnabled && _addressSettings.CompanyRequired && !address.Company.HasValue())
                return false;

            if (_addressSettings.StreetAddressEnabled && _addressSettings.StreetAddressRequired && !address.Address1.HasValue())
                return false;

            if (_addressSettings.StreetAddress2Enabled && _addressSettings.StreetAddress2Required && !address.Address2.HasValue())
                return false;

            if (_addressSettings.ZipPostalCodeEnabled && _addressSettings.ZipPostalCodeRequired && !address.ZipPostalCode.HasValue())
                return false;

            if (_addressSettings.CountryEnabled)
            {
                if (address.CountryId == null || address.CountryId.Value == 0)
                    return false;

                var country = await _db.Countries.FindByIdAsync(address.CountryId.Value, false);

                if (country == null)
                    return false;

                if (_addressSettings.StateProvinceEnabled)
                {
                    var hasStates = await _db.StateProvinces
                        .AsNoTracking()
                        .ApplyCountryFilter(country.Id)
                        .AnyAsync();
                        
                    if (hasStates)
                    {
                        if (address.StateProvinceId == null || address.StateProvinceId.Value == 0)
                            return false;

                        var state = await _db.StateProvinces.FindByIdAsync(address.StateProvinceId.Value, false);
                        if (state == null)
                            return false;
                    }
                }
            }

            if (_addressSettings.CityEnabled && _addressSettings.CityRequired && !address.City.HasValue())
                return false;

            if (_addressSettings.PhoneEnabled && _addressSettings.PhoneRequired && !address.PhoneNumber.HasValue())
                return false;

            if (_addressSettings.FaxEnabled && _addressSettings.FaxRequired && address.FaxNumber.HasValue())
                return false;

            return true;
        }

        public virtual async Task<string> FormatAddressAsync(CompanyInformationSettings settings, bool newLineToBr = false)
        {
            Guard.NotNull(settings, nameof(settings));

            var address = new Address
            {
                Address1 = settings.Street,
                Address2 = settings.Street2,
                City = settings.City,
                Company = settings.CompanyName,
                FirstName = settings.Firstname,
                LastName = settings.Lastname,
                Salutation = settings.Salutation,
                Title = settings.Title,
                ZipPostalCode = settings.ZipCode,
                CountryId = settings.CountryId,
                Country = await _db.Countries.FindByIdAsync(settings.CountryId, false)
            };

            return await FormatAddressAsync(address, newLineToBr);
        }

        public virtual async Task<string> FormatAddressAsync(Address address, bool newLineToBr = false)
        {
            Guard.NotNull(address, nameof(address));

            var messageContext = new MessageContext
            {
                Language = _services.WorkContext.WorkingLanguage,
                Store = _services.StoreContext.CurrentStore,
                Model = new TemplateModel()
            };

            await _messageModelProvider.AddModelPartAsync(address, messageContext, "Address");
            var model = messageContext.Model["Address"];

            var result = FormatAddress(model, address?.Country?.AddressFormat, messageContext.FormatProvider);

            if (newLineToBr)
            {
                result = HtmlUtils.ConvertPlainTextToHtml(result);
            }

            return result;
        }

        public virtual string FormatAddress(object address, string template = null, IFormatProvider formatProvider = null)
        {
            Guard.NotNull(address, nameof(address));

            template = template.NullEmpty() ?? Address.DefaultAddressFormat;

            var result = _templateEngine
                .Render(template, address, formatProvider ?? CultureInfo.CurrentCulture)
                .Compact(true);

            return result;
        }
    }
}
