# Tax

## Overview

Smartstore abstracts tax calculation into two parts: a _provider_ resolves the tax rate and a _calculator_ applies that rate to prices. Everything is driven by `TaxSettings` which determine whether amounts are entered and displayed inclusive or exclusive of tax.

### Tax categories

Every product is assigned to a **tax category** so the system knows which rate to apply. Categories are simple records with a name and display order and can be edited in the admin area under **Configuration → Taxes**.

### Tax providers

The active provider is selected via `ITaxService` and returns a `TaxRate` for a given `TaxRateRequest`. Smartstore ships with a fixed rate provider and a region‑based provider in the _Smartstore.Tax_ module, but you can implement your own by registering an `ITaxProvider`.

```csharp
public class CustomTaxProvider : ITaxProvider
{
    public Task<TaxRate> GetTaxRateAsync(TaxRateRequest request)
    {
        // return 7% for books, otherwise 19%
        var rate = request.TaxCategoryId == 5 ? 7m : 19m;
        return Task.FromResult(new TaxRate(rate, request.TaxCategoryId));
    }
}
```

### Calculating tax

`ITaxCalculator` exposes helpers for products, checkout attributes, shipping and payment fees. The calculator looks up the applicable rate, applies rounding and returns a `Tax` structure containing both net and gross values.

```csharp
public async Task<Money> GetGrossPriceAsync(Product product, decimal netPrice)
{
    var tax = await _taxCalculator.CalculateProductTaxAsync(product, netPrice, inclusive: true);
    return _taxService.ApplyTaxFormat(tax.Price);
}
```

### Display and formatting

To append legal suffixes such as “incl. VAT” use `ITaxService.ApplyTaxFormat` and its shipping/payment variants. Suffix visibility and whether prices include tax are governed by `TaxSettings.DisplayTaxSuffix` and `TaxSettings.PricesIncludeTax` and must be passed to the method `ITaxService.ApplyTaxFormat` to take effect.

### VAT numbers and exemptions

`ITaxService` also validates EU VAT numbers and checks whether a product or customer is tax exempt:

```csharp
var vat = await _taxService.GetVatNumberStatusAsync("DE123456789");
bool exempt = await _taxService.IsVatExemptAsync(customer);
```

### Tax settings

Injecting `TaxSettings` gives access to configuration such as the default tax address, whether shipping or payment fees are taxable and which provider is active.
