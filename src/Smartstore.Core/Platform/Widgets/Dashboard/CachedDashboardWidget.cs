using Microsoft.AspNetCore.Html;
using Smartstore.Caching;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Caches the rendered HTML of a dashboard widget for its settings and authorized display context.
/// </summary>
public sealed class CachedDashboardWidget : Widget
{
    private readonly Widget _widget;
    private readonly string _cacheKey;
    private readonly bool _refresh;
    private readonly TimeSpan _cacheDuration;
    private readonly ICacheManager _cache;

    /// <summary>
    /// Wraps a dashboard widget without changing its component arguments.
    /// </summary>
    public CachedDashboardWidget(
        Widget widget,
        string cacheKey,
        bool refresh,
        TimeSpan cacheDuration,
        ICacheManager cache)
    {
        Guard.NotNull(widget);
        Guard.NotEmpty(cacheKey);
        Guard.IsPositive(cacheDuration.Ticks, nameof(cacheDuration));
        Guard.NotNull(cache);

        _widget = widget;
        _cacheKey = cacheKey;
        _refresh = refresh;
        _cacheDuration = cacheDuration;
        _cache = cache;

        Key = widget.Key;
        Order = widget.Order;
        Prepend = widget.Prepend;
    }

    /// <inheritdoc />
    public override async Task<IHtmlContent> InvokeAsync(WidgetContext context)
    {
        if (_refresh)
        {
            await _cache.RemoveAsync(_cacheKey);
        }

        string html = await _cache.GetAsync(_cacheKey, async entry =>
        {
            entry.ExpiresIn(_cacheDuration);
            var content = await _widget.InvokeAsync(context);
            return content.ToHtmlString().Value ?? string.Empty;
        });

        return new HtmlString(html);
    }
}
