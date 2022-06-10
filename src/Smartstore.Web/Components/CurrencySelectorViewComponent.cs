using Smartstore.Core.Localization;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class CurrencySelectorViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;

        public CurrencySelectorViewComponent(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var key = ModelCacheInvalidator.AVAILABLE_CURRENCIES_MODEL_KEY.FormatInvariant(Services.WorkContext.WorkingLanguage.Id, Services.StoreContext.CurrentStore.Id);
            var availableCurrencies = await Services.CacheFactory.GetMemoryCache().GetAsync(key, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(24));

                var currencies = await _db.Currencies
                    .AsNoTracking()
                    .ApplyStandardFilter(false, Services.StoreContext.CurrentStore.Id)
                    .ToListAsync();

                var result = currencies
                    .Select(x => new CurrencyModel
                    {
                        Id = x.Id,
                        Name = x.GetLocalized(y => y.Name, false),
                        ISOCode = x.CurrencyCode,
                        Symbol = CultureHelper.GetCurrencySymbol(x.DisplayLocale) ?? x.CurrencyCode
                    })
                    .ToList();

                return result;
            });

            if (availableCurrencies.Count < 2)
            {
                return Empty();
            }

            ViewBag.AvailableCurrencies = availableCurrencies;

            return View();
        }
    }
}
