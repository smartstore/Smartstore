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

## Character conversion

To keep URLs readable across languages, Smartstore converts special characters according to `SeoNameCharConversion`. Set `ConvertNonWesternChars` to `false` or `AllowUnicodeCharsInUrls` to permit full Unicode slugs when appropriate.
