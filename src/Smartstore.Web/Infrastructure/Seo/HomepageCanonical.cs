using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Seo;
using Smartstore.Core.Widgets;

namespace Smartstore.Web.Infrastructure.Seo;

/// <summary>
/// Adds a canonical URL for the storefront homepage when canonical links are enabled.
/// </summary>
public static class HomepageCanonical
{
    public static void Register(IPageAssetBuilder assets, SeoSettings settings, IUrlHelper url, string scheme)
    {
        if (!settings.CanonicalUrlsEnabled)
        {
            return;
        }

        // Always resolve the named homepage route so a query string or an alternative
        // localized homepage URL cannot accidentally become the canonical address.
        var canonicalUrl = url.RouteUrl("Homepage", new { }, scheme);
        if (!string.IsNullOrWhiteSpace(canonicalUrl))
        {
            assets.AppendCanonicalUrlParts(canonicalUrl);
        }
    }
}
