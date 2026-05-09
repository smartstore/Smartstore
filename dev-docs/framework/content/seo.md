# SEO

Search engine optimization in Smartstore revolves around human readable URLs, meta data and machine consumable resources such as `robots.txt` and the XML sitemap. The framework ships with sensible defaults but allows full customization through services and settings.

## URL slugs

Entities that implement `ISlugSupported` (products, categories, topics, manufacturers ...) persist their slugs in the `UrlRecord` table, one per language. The `IUrlService` validates and applies unique slugs and keeps historical entries for 301 redirects when a slug changes.

```csharp
var result = await _urlService.ValidateSlugAsync(product,
    seName: myNewSlug,
    displayName: product.Name,
    ensureNotEmpty: true);
await _urlService.ApplySlugAsync(result, save: true);
```

## Meta information

`SeoSettings` provides global defaults for `MetaTitle`, `MetaDescription` and `MetaKeywords`. Most storefront entities expose matching properties so editors can override them per language in the administration area.

## Canonical URLs and policies

`UrlPolicy` compares each request against `SeoSettings` (host rule, HTTPS, trailing slash) and triggers a permanent redirect when needed. Enabling `CanonicalUrlsEnabled` renders a canonical `<link>` tag, and `AddAlternateHtmlLinks` injects `hreflang` links for localized pages.

## Robots and sitemaps

The `robots.txt` endpoint merges `SeoSettings.DefaultRobotDisallows` with `ExtraRobotsDisallows`, `ExtraRobotsAllows` and optional `ExtraRobotsLines` to control crawler access.

`IXmlSitemapPublisher` builds `sitemap.xml` and can include categories, manufacturers, products, topics, blog, news and forum entries according to the `XmlSitemapIncludes*` flags in settings. Alternate links per language are emitted when `XmlSitemapIncludesAlternateLinks` is enabled.

## JSON-LD structured data

`JsonLdBuilder` (accessible via `Assets.JsonLd` in views) collects typed JSON-LD fragments per page and renders them as `<script type="application/ld+json">` blocks. Each fragment represents one schema.org type and exposes a fluent API.

Shortcut properties exist for the most common root types: `Assets.JsonLd.Product`, `.BreadcrumbList`, `.BlogPosting`, `.Organization`, `.WebSite`. Each call to a shortcut returns the same fragment for that type, so multiple partials can contribute incrementally.

```csharp
// In a Razor partial — contribute from anywhere, order doesn't matter
Assets.JsonLd.Product
    .Prop("name", Model.Name)
    .Prop("sku", Model.Sku)
    .Obj("brand", "Brand", new { name = Model.Brand })
    .Obj("offers", "Offer", new
    {
        price = Model.Price.FinalPrice,
        priceCurrency = Model.Price.CurrencyCode
    });
```

Anonymous objects use `type` (without `@`) as a convention — the framework normalizes it to `@type` automatically, so you never have to write `@`-prefixed keys in C# anonymous types.

For nested arrays (e.g. `shippingDetails`), build the items explicitly and use `Arr(...)`:

```csharp
var offer = JsonLdFragment.Create("Offer", new { price = ..., priceCurrency = ... });

var items = shippingList.Select(s => JsonLdFragment.Create("OfferShippingDetails", new
{
    shippingDestination = new { type = "DefinedRegion", addressCountry = s.CountryCode },
    shippingRate = new { type = "MonetaryAmount", value = s.Cost, currency = s.CurrencyCode }
}));

offer.Arr("shippingDetails", items);
Assets.JsonLd.Product.Obj("offers", offer);
```

> Structured data is always emitted regardless of UI visibility flags — if the data exists, include it.

> Properties with `null` values are silently ignored and never written to the output.

## Character conversion

To keep URLs readable across languages, Smartstore converts special characters according to `SeoNameCharConversion`. Set `ConvertNonWesternChars` to `false` or `AllowUnicodeCharsInUrls` to permit full Unicode slugs when appropriate.
