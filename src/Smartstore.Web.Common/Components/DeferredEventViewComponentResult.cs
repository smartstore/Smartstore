using Microsoft.AspNetCore.Mvc.ViewComponents;
using Smartstore.Events;

namespace Smartstore.Web.Components;

/// <summary>
/// Wraps an <see cref="IViewComponentResult"/> and defers the publishing of
/// <see cref="ViewComponentResultExecutingEvent"/> to <see cref="ExecuteAsync"/>,
/// where async execution is natively supported by the framework.
/// This avoids sync-over-async when publishing the event from synchronous result factory methods.
/// </summary>
internal sealed class DeferredEventViewComponentResult : IViewComponentResult
{
    private readonly ViewComponentContext _componentContext;
    private readonly IEventPublisher _eventPublisher;
    private IViewComponentResult _inner;

    public DeferredEventViewComponentResult(
        IViewComponentResult inner,
        ViewComponentContext componentContext,
        IEventPublisher eventPublisher)
    {
        _inner = inner;
        _componentContext = componentContext;
        _eventPublisher = eventPublisher;
    }

    public void Execute(ViewComponentContext context)
    {
        var e = new ViewComponentResultExecutingEvent(_componentContext, _inner);
        _eventPublisher.Publish(e);

        if (e.Result != null && !ReferenceEquals(e.Result, _inner))
        {
            _inner = e.Result;
        }

        _inner.Execute(context);
    }

    public async Task ExecuteAsync(ViewComponentContext context)
    {
        var e = new ViewComponentResultExecutingEvent(_componentContext, _inner);
        await _eventPublisher.PublishAsync(e);

        if (e.Result != null && !ReferenceEquals(e.Result, _inner))
        {
            _inner = e.Result;
        }

        await _inner.ExecuteAsync(context);
    }
}
