# Templating

## Overview
Smartstore renders dynamic content with the [DotLiquid](https://github.com/dotliquid/dotliquid) engine. Liquid syntax powers email bodies and other text snippets and can be extended with custom filters or tags. Templates are executed against arbitrary models and support localization out of the box.

## Template files
Liquid templates use the `.liquid` extension. When a path starts with `~/` it is resolved against the application root; otherwise the current template is searched under `Views/Shared/EmailTemplates`.

```
Themes/<ThemeName>/Views/Shared/EmailTemplates/order.liquid
```

## Rendering templates
Inject `ITemplateEngine` into a service and render a template string or file with a model:

```csharp
public class OrderEmailBuilder
{
    private readonly ITemplateEngine _templates;
    public OrderEmailBuilder(ITemplateEngine templates) => _templates = templates;

    public async Task<string> BuildAsync(Order order)
    {
        var source = "Hello {{ Customer.FirstName }}, your order {{ Order.CustomOrderNumber }} totals {{ Order.OrderTotal }}.";
        var model = new { Order = order, Customer = order.Customer };
        return await _templates.RenderAsync(source, model);
    }
}
```

For previews without real data, `ITemplateEngine.CreateTestModelFor` can generate a model populated with dummy values.


## Injecting into zones
A custom `zone` tag lets modules inject content into well‑known spots inside a template:

```liquid
{% zone 'order_items_before' %}
```

Subscribe to `TemplateZoneRenderingEvent` to provide HTML or Liquid fragments for a zone:

```csharp
public class ThankYouHandler : IConsumer<TemplateZoneRenderingEvent>
{
    public Task HandleEventAsync(TemplateZoneRenderingEvent ev)
    {
        if (ev.ZoneName == "order_items_before")
        {
            ev.InjectContent("<p>Thank you for your order!</p>", parse: false);
        }
        return Task.CompletedTask;
    }
}
```