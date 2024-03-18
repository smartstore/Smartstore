using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Messaging;
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
            return Task.FromResult(FixAddress(entity));
        }

        protected override Task<HookResult> OnUpdatingAsync(Address entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            return Task.FromResult(FixAddress(entity));
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var addressIds = entries
                .Select(x => x.Entity)
                .OfType<Address>()
                .ToDistinctArray(x => x.Id);

            if (addressIds.Length > 0)
            {
                var orders = await _db.Orders
                    .Where(x => (x.BillingAddressId != null && addressIds.Contains(x.BillingAddressId.Value)) || (x.ShippingAddressId != null && addressIds.Contains(x.ShippingAddressId.Value)))
                    .ToListAsync(cancelToken);

                foreach (var order in orders)
                {
                    await _services.EventPublisher.PublishOrderUpdatedAsync(order);
                }
            }
        }

        private static HookResult FixAddress(Address address)
        {
            if (address.CountryId == 0)
                address.CountryId = null;

            if (address.StateProvinceId == 0)
                address.StateProvinceId = null;

            return HookResult.Ok;
        }

        #endregion

        public virtual async Task<bool> IsAddressValidAsync(Address address)
        {
            Guard.NotNull(address);

            if (!address.FirstName.HasValue() || 
                !address.LastName.HasValue() || 
                !address.Email.HasValue())
                return false;

            if (_addressSettings.CompanyEnabled &&
                _addressSettings.CompanyRequired &&
                !address.Company.HasValue())
                return false;

            if (_addressSettings.StreetAddressEnabled &&
                _addressSettings.StreetAddressRequired &&
                !address.Address1.HasValue())
                return false;

            if (_addressSettings.StreetAddress2Enabled &&
                _addressSettings.StreetAddress2Required &&
                !address.Address2.HasValue())
                return false;

            if (_addressSettings.ZipPostalCodeEnabled &&
                _addressSettings.ZipPostalCodeRequired &&
                !address.ZipPostalCode.HasValue())
                return false;

            if (_addressSettings.CityEnabled &&
                _addressSettings.CityRequired &&
                !address.City.HasValue())
                return false;

            if (_addressSettings.PhoneEnabled &&
                _addressSettings.PhoneRequired &&
                !address.PhoneNumber.HasValue())
                return false;

            if (_addressSettings.FaxEnabled &&
                _addressSettings.FaxRequired &&
                address.FaxNumber.HasValue())
                return false;

            if (_addressSettings.CountryEnabled)
            {
                if (address.CountryId == null || address.CountryId.Value == 0)
                    return false;

                var country = await _db.Countries
                    .Include(x => x.StateProvinces.OrderBy(x => x.DisplayOrder))
                    .FindByIdAsync(address.CountryId.Value, false);

                if (country == null)
                    return false;

                if (_addressSettings.StateProvinceEnabled)
                {
                    var hasStates = country.StateProvinces.Any();

                    if (hasStates)
                    {
                        if (address.StateProvinceId == null || address.StateProvinceId.Value == 0)
                            return false;

                        var state = country.StateProvinces.FirstOrDefault(x => address.StateProvinceId.Value == x.Id);
                        if (state == null)
                            return false;
                    }
                }
            }

            return true;
        }

        public virtual async Task<string> FormatAddressAsync(CompanyInformationSettings settings, bool newLineToBr = false)
        {
            Guard.NotNull(settings);

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
            Guard.NotNull(address);

            var messageContext = new MessageContext
            {
                Language = _services.WorkContext.WorkingLanguage,
                Store = _services.StoreContext.CurrentStore,
                Model = new TemplateModel()
            };

            await _messageModelProvider.AddModelPartAsync(address, messageContext, "Address");
            var model = messageContext.Model["Address"];

            var result = await FormatAddressAsync(model, address?.Country?.AddressFormat, messageContext.FormatProvider);

            if (newLineToBr)
            {
                result = HtmlUtility.SanitizeHtml(HtmlUtility.ConvertPlainTextToHtml( result), true);
            }

            return result;
        }

        public virtual async Task<string> FormatAddressAsync(object address, string template = null, IFormatProvider formatProvider = null)
        {
            Guard.NotNull(address);

            template = template.NullEmpty() ?? Address.DefaultAddressFormat;

            var result = await _templateEngine
                .RenderAsync(template, address, formatProvider);

            return result.Compact(true);
        }
    }
}
