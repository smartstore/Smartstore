using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
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
            var availableCurrencies = await Services.Cache.GetAsync(key, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(24));

                // TODO: (mh) (core) is this correct? Doesn't feel so :-)
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

            ViewBag.AvailableCurrencies = availableCurrencies;

            return View();
        }
    }
}
