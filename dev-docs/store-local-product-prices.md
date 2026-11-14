# Store-local product prices — issue #1596

## Purpose

An optional merchant-entered regular product price may be saved independently for each store, in the store's configured default currency. E.g. a global base price of EUR 34.90, HUF 12,990 in Hungary, and CZK 849 in Czechia.

## Compatibility and data model

- Existing `Product.Price` remains in the global primary currency and is never rewritten by this feature.
- `ProductStorePrice` stores one row per product/store, with the native entered `Price` and an explicit `CurrencyId`. The database enforces a unique product/store pair and valid product/store/currency relationships.
- Absence of an override retains the original global price with the original exchange conversion; no migration or backfill of product prices occurs.
- If a store's default currency changes, the old price remains safely stored under the old currency ID but becomes inactive. The admin screen displays a warning and requires a newly entered price for the new currency.
- A stale editing form with an out-of-date currency ID is rejected instead of reinterpreting the submitted amount.
- Nulling a price in its valid current currency removes the override; nulling an inactive stale-price field does not silently delete it.
- Rollback does not drop merchant-entered prices automatically.

## Calculation

The store-specific native price is converted to the global primary currency **at the price-calculation boundary**, so existing attribute, tier, discount, bundle and tax calculations keep their established pipeline. Final customer-currency conversion uses the normal currency service. Native prices are not recalculated or overwritten when exchange rates change.

Nested bundle and grouped-product calculations use the child-product batch and the same currency service. Tier percentage/adjustment calculations use the effective store-specific base rather than the global `Product.Price`; existing APIs and behavior remain unchanged when no override exists.

## Administration

The Price tab contains an optional override field for each configured store, labeled with the store name and default currency. A blank value means use the global product price. Inputs are constrained to non-negative amounts representable in DECIMAL(18,4). The server validates the set of stores, disallows duplicated store rows and checks currency identity before persistence.

## Verification

`PriceCalculationServiceTests` covers:
- exact per-store prices across two stores and currencies;
- fixed local price after exchange-rate changes, including fractional rates;
- safe fallback in other stores and after a store currency change;
- nested bundle item pricing;
- all preexisting calculations for tier prices, discounts, variant combinations, shopping-cart totals and other pricing paths.

The `Smartstore.Core.Tests` suite and Release web build should be run before release. The migration must additionally be tested on each supported real database provider before enabling the feature in a production storefront.

## Review notes / boundaries

This adds an *optional regular product price* override, not independently configurable special prices, compare prices, per-store tier prices or per-store variant-combination prices. These existing mechanisms continue to use their original definitions and compose with the new base price. Advanced price-index-based sorting and search depend on the existing catalog search-index representation and merit separate review to ensure parity with storefront display for large catalogs.

Do not deploy to production before a maintainer reviews business rules for tax-inclusive configurations, currency rounding, concurrent edits, and catalog search indexing. Use an isolated database with representative products for that validation.
