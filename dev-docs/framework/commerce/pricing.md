# Pricing

## Overview
Smartstore calculates product prices through a pipeline of **price calculators**. Each calculator adjusts the running price for a specific concern such as tier prices, discounts or attribute surcharges. The pipeline is orchestrated by `IPriceCalculationService` and produces a `CalculatedPrice` that contains final and regular amounts, discounts, taxes and more.

## Calculating a product price
Create default options, build a calculation context and ask the service to compute the price:

```csharp
var options = _priceCalculationService.CreateDefaultOptions(forListing: false);
var context = new PriceCalculationContext(product, quantity: 1, options);
var price = await _priceCalculationService.CalculatePriceAsync(context);

Money amount = price.FinalPrice;
```

`CalculateSubtotalAsync` returns both unit price and subtotal when a quantity greater than one is involved. For shopping cart items you can let the service prepare the context which includes selected attributes and bundle information:

```csharp
var options = _priceCalculationService.CreateDefaultOptions(forListing: false);
var cartItemContext = await _priceCalculationService.CreateCalculationContextAsync(cartItem, options);
var (unitPrice, subtotal) = await _priceCalculationService.CalculateSubtotalAsync(cartItemContext);
```

## Price calculation options
`PriceCalculationOptions` controls pipeline behaviour. Important flags include:

- `TaxInclusive` – whether returned amounts include tax.
- `IgnoreDiscounts` – skip discount evaluation.
- `DetermineLowestPrice` – look for the cheapest child product or tier price and mark ranges.
- `DeterminePreselectedPrice` – apply attribute combinations preselected by the merchant.
- `DeterminePriceAdjustments` – calculate attribute price adjustments and expose them via `CalculatedPrice.AttributePriceAdjustments`.
- `RoundingCurrency` – currency used when rounding amounts.

The `CreateDefaultOptions` helper pulls sensible defaults from the work and store context so most callers only tweak the flags they need.

## Attribute adjustments and base prices
Use the extension methods to query additional pricing information:

```csharp
// Price difference for selected attribute values
var adjustments = await _priceCalculationService
    .CalculateAttributePriceAdjustmentsAsync(product, selection);

// Human readable base price (e.g. "24,90 € / 100 g")
var basePriceInfo = await _priceCalculationService.GetBasePriceInfoAsync(product);
```

## Custom price calculators
The pipeline is extensible. To add custom charges or discounts implement `IPriceCalculator` and decorate it with `CalculatorUsage` to specify supported product types and order.

```csharp
[CalculatorUsage(CalculatorTargets.Product, order: 500)]
public class EnvironmentalFeeCalculator : PriceCalculator
{
    public EnvironmentalFeeCalculator(IPriceCalculatorFactory factory)
        : base(factory) { }

    protected override Task ComputePriceAsync(CalculatorContext context)
    {
        context.WorkingPrice += new Money(1.50m, context.TargetCurrency);
        return Task.CompletedTask;
    }
}
```

Register the calculator through DI. When the service runs, your calculator participates in the pipeline and adjusts the `WorkingPrice` before subsequent calculators are executed.