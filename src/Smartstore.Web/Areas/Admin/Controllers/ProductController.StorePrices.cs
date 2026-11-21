using Smartstore.Admin.Models.Catalog;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Common;
using Smartstore.Core.Stores;

namespace Smartstore.Admin.Controllers;

public partial class ProductController
{
    private async Task PrepareStorePricesAsync(ProductModel model, int productId)
    {
        var stores = Services.StoreContext.GetAllStores().OrderBy(x => x.Name).ToList();
        var prices = await _db.ProductStorePrices
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToDictionaryAsync(x => x.StoreId);
        var currencies = await _db.Currencies
            .AsNoTracking()
            .Where(x => stores.Select(s => s.DefaultCurrencyId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        model.StorePrices = stores
            .Where(x => currencies.ContainsKey(x.DefaultCurrencyId))
            .Select(store =>
            {
                prices.TryGetValue(store.Id, out var saved);
                return new ProductModel.ProductStorePriceModel
                {
                    StoreId = store.Id,
                    CurrencyId = store.DefaultCurrencyId,
                    StoreName = store.Name,
                    CurrencyCode = currencies[store.DefaultCurrencyId].CurrencyCode,
                    CurrencyChanged = saved != null && saved.CurrencyId != store.DefaultCurrencyId,
                    Price = saved?.CurrencyId == store.DefaultCurrencyId ? saved.Price : null
                };
            })
            .ToList();
    }

    private async Task ValidateStorePricesAsync(ProductModel model)
    {
        if (!model.IsTabLoaded("Price") || model.StorePrices == null)
        {
            return;
        }

        var rows = model.StorePrices;
        if (rows.Select(x => x.StoreId).Distinct().Count() != rows.Count)
        {
            ModelState.AddModelError(nameof(model.StorePrices), "Duplicate store price rows are not allowed.");
            return;
        }

        var stores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
        var currencies = await _db.Currencies
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id);

        foreach (var row in rows)
        {
            if (!stores.TryGetValue(row.StoreId, out var store) ||
                !currencies.TryGetValue(store.DefaultCurrencyId, out var currency))
            {
                ModelState.AddModelError(nameof(model.StorePrices), "A store or its default currency is invalid.");
                continue;
            }

            if (row.CurrencyId != store.DefaultCurrencyId)
            {
                ModelState.AddModelError(nameof(model.StorePrices), "The store currency changed while editing. Reload the product before saving local prices.");
                continue;
            }

            if (row.Price is not decimal amount)
            {
                continue;
            }

            if (amount < 0m || amount > 99999999999999.9999m || decimal.Round(amount, 4) != amount)
            {
                ModelState.AddModelError(nameof(model.StorePrices), "A local price must be non-negative and have at most four decimal places.");
                continue;
            }

            if (currency.Id != _currencyService.PrimaryCurrency.Id && currency.Rate <= 0m)
            {
                ModelState.AddModelError(nameof(model.StorePrices), "The store currency must have a positive exchange rate.");
            }
        }
    }

    private async Task UpdateStorePricesAsync(Smartstore.Core.Catalog.Products.Product product, ProductModel model)
    {
        if (model.StorePrices == null || model.StorePrices.Count == 0)
        {
            return;
        }

        var stores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
        var stored = await _db.ProductStorePrices
            .Where(x => x.ProductId == product.Id)
            .ToDictionaryAsync(x => x.StoreId);

        foreach (var row in model.StorePrices)
        {
            var currencyId = stores[row.StoreId].DefaultCurrencyId;
            stored.TryGetValue(row.StoreId, out var existing);

            if (row.Price == null)
            {
                if (existing != null && existing.CurrencyId == currencyId)
                {
                    _db.ProductStorePrices.Remove(existing);
                }
            }
            else if (existing != null)
            {
                existing.CurrencyId = currencyId;
                existing.Price = row.Price.Value;
            }
            else
            {
                _db.ProductStorePrices.Add(new ProductStorePrice
                {
                    ProductId = product.Id,
                    StoreId = row.StoreId,
                    CurrencyId = currencyId,
                    Price = row.Price.Value
                });
            }
        }
    }
}
