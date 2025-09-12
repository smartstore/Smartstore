---
description: Pub/sub system for loosely coupled communication
---

# ✔️ Events

## Overview

The application publishes event messages on various occasions, such as when a customer signs in or registers, places or pays an order, or performs a catalog search. These event messages can be of any complex type and do not have to adhere to any specific interface or base class. You can consume these event messages from anywhere, including from your custom modules.

However, there are two interfaces that are important for consuming and publishing events (which we will discuss in more detail later in this topic):

* [IEventPublisher](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Events/IEventPublisher.cs) interface is responsible for dispatching event messages to subscribers.
* [IConsumer](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Events/IConsumer.cs) interface makes a class a _consumer_ (aka handler or subscriber) for one or more events.

## Consuming Events

Event handler methods are used to perform pre- or post-processing tasks for an event. These methods must meet the following criteria:

* Be public
* Be non-static
* Have a `void` or `Task` return type
* Follow the naming conventions:
  * For async handlers: `HandleAsync, HandleEventAsync` or `ConsumeAsync.`
  * For sync handlers: `Handle, HandleEvent` or `Consume`

The first parameter of the method must **always** be the event message, or an instance of [ConsumeContext\<TMessage>](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Events/ConsumeContext.cs).&#x20;

The [IConsumerInvoker](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Events/IConsumerInvoker.cs) interface decides how to call the method based on its signature:

* `void` methods are invoked synchronously
* `Task` methods are invoked asynchronously and awaited
* with the [FireForgetAttribute](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Events/FireForgetAttribute.cs) the method is executed in the background without awaiting. This can be advantageous in long running processes, because the current request thread is not _blocked_.

{% hint style="warning" %}
Use `FireForgetAttribute` with caution and only if you know what you are doing 😊. A class that includes a _Fire & forget_ consumer should **not** take dependencies on request scoped services, because task continuation happens on another thread, and context gets lost. Instead, pass the required dependencies as method parameters. The consumer invoker spawns a new private context for the unit of work and resolves dependencies from this context.
{% endhint %}

You can declare additional dependency parameters in the handler method:

```csharp
public async Task HandleEventAsync(SomeEvent message, 
    IDbContext db, 
    ICacheManager cache, 
    CancellationToken cancelToken) 
{
    // Your code
}
```

Order of parameters does not matter. The invoker automatically resolves the appropriate instances and passes them to the method. Any unregistered dependency or a primitive type throws an exception, except for `CancellationToken`, which always resolves to the application shutdown token.&#x20;

All types that implement the `IConsumer` interface are automatically detected on application startup and there is no need to register them in the service container. The class itself is registered as a _scoped dependency_, so it can also take dependencies in the constructor.

{% hint style="info" %}
**TIP:** If there are multiple handler methods present in the consumer class, you can pass shared dependencies in the class constructor. Otherwise, use method parameters.
{% endhint %}

For example, the [ValidatingCartEventConsumer](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Checkout/Cart/Consumers/ValidatingCartEventConsumer.cs) class contains a `HandleEventAsync` implementation. The method receives a `ValidatingCartEvent` message that contains the shopping cart context as well as any warnings. The method validates the cart context and adds warnings to the message whenever the cart total is below the minimum or above the maximum allowed amount.

```csharp
internal class ValidatingCartEventConsumer : IConsumer
{
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly ILocalizationService _localizationService;
    private readonly ICurrencyService _currencyService;
    private readonly IWorkContext _workContext;

    public ValidatingCartEventConsumer(
        IOrderProcessingService orderProcessingService,
        ILocalizationService localizationService,
        ICurrencyService currencyService,
        IWorkContext workContext)
    {
        _orderProcessingService = orderProcessingService;
        _localizationService = localizationService;
        _currencyService = currencyService;
        _workContext = workContext;
    }

    public async Task HandleEventAsync(ValidatingCartEvent message)
    {
        // Order total validation.
        var roleMappings = _workContext.CurrentImpersonator?.CustomerRoleMappings 
            ?? message.Cart.Customer.CustomerRoleMappings;
        var result = await _orderProcessingService
            .ValidateOrderTotalAsync(message.Cart, roleMappings
                .Select(x => x.CustomerRole).ToArray());

        if (!result.IsAboveMinimum)
        {
            var convertedMin = _currencyService.ConvertFromPrimaryCurrency(result.OrderTotalMinimum, _workContext.WorkingCurrency);
            message.Warnings.Add(_localizationService.GetResource("Checkout.MinOrderSubtotalAmount").FormatInvariant(convertedMin.ToString(true)));
        }

        if (!result.IsBelowMaximum)
        {
            var convertedMax = _currencyService.ConvertFromPrimaryCurrency(result.OrderTotalMaximum, _workContext.WorkingCurrency);
            message.Warnings.Add(_localizationService.GetResource("Checkout.MaxOrderSubtotalAmount").FormatInvariant(convertedMax.ToString(true)));
        }
    }
}
```

{% hint style="info" %}
**For module developers:** It is good practice to add a file _Events.cs_ to the root of the module and implement all handler methods in it. The class should be internal. If it becomes too large, you should split/group the methods: either many or just partial classes.
{% endhint %}

## Publishing events

To publish an event, you will need to create an event message of any type and populate it with the necessary data. Use the [IEventPublisher](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Events/IEventPublisher.cs) service, which provides the `PublishAsync` method for publishing an event and dispatching the message to all subscribers of that event.

{% hint style="warning" %}
Don't call the synchronous `Publish` method, unless you absolutely cannot avoid it. It blocks the thread if any subscriber has _real_ asynchronous code.
{% endhint %}

In the next example, the `ValidatingCartEvent` is published in the `Index` method of the [CheckoutController](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Web/Controllers/CheckoutController.cs). This method sends the current status of the cart and a list of any warnings that may have occurred. The same event is also handled in the previously mentioned example.

<pre class="language-csharp"><code class="lang-csharp">// ...
var storeId = _storeContext.CurrentStore.Id;
var customer = _workContext.CurrentCustomer;
var cart = await _shoppingCartService.GetCartAsync(customer, storeId: storeId);

if (!cart.Items.Any())
{
    return RedirectToRoute("ShoppingCart");
}

if (customer.IsGuest() &#x26;&#x26; !_orderSettings.AnonymousCheckoutAllowed)
{
    return new UnauthorizedResult();
}

// Validate checkout attributes.
var warnings = new List&#x3C;string>();
if (!await _shoppingCartValidator.ValidateCartAsync(cart, warnings, true))
{    
    warnings.Take(3).Each(x => NotifyWarning(x));
    return RedirectToRoute("ShoppingCart");
}

// Create event message...
<strong>var validatingCartEvent = new ValidatingCartEvent(cart, warnings);
</strong><strong>
</strong><strong>// ...and publish
</strong><strong>await _eventPublisher.PublishAsync(validatingCartEvent);
</strong>// ...
</code></pre>

## Message Bus

A message bus can be used for inter-server communication between nodes in a web farm, which is a group of servers that work together to host a website or application. In Smartstore, the [IMessageBus](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Events/IMessageBus.cs) service represents the message bus system. It activates when, for example, the REDIS plugin is installed, because the plugin delivers a message bus provider. By default it falls back to `NullMessageBus`, which actually does nothing.

Messages sent through a message bus must be simple `string` values and do not support complex data types. It is guaranteed that the server that published a message will not consume it, meaning that the message will only be passed along to other nodes for processing.

The following example shows the [MemoryCacheStore](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Caching/MemoryCacheStore.cs) class. The constructor subscribes to a channel in message bus called _cache_. The `Subscribe` method accepts the channel name and the handler method (`OnCacheEvent` in this case) that handles the message.

<pre class="language-csharp"><code class="lang-csharp">public MemoryCacheStore(IOptions&#x3C;MemoryCacheOptions> optionsAccessor, 
    IMessageBus bus, 
    ILoggerFactory loggerFactory)
{
    _optionsAccessor = optionsAccessor;
    _bus = bus;
    _loggerFactory = loggerFactory;

    _cache = CreateCache();

    // Subscribe to cache events sent by other nodes in a web farm
<strong>    _bus.Subscribe("cache", OnCacheEvent);
</strong>}

// ...

private void OnCacheEvent(string channel, string message)
{
    var parameter = string.Empty;
    string action;

    var index = message.IndexOf('^');
    if (index >= 0 &#x26;&#x26; index &#x3C; message.Length - 1)
    {
        action = message[..index];
        parameter = message[(index + 1)..];
    }
    else
    {
        action = message;
    }

    switch (action)
    {
        case "clear":
            Clear();
            break;
        case "remove":
            Remove(parameter);
            break;
        case "removebypattern":
            RemoveByPattern(parameter);
            break;
    }
}
</code></pre>

## List of all core events

All event messages in alphabetical order. The _Event_ suffix is omitted for brevity. Modules may provide more events than listed here. This is not a complete reference. Analyze the corresponding classes in the source code to learn more about properties and usage.

<table><thead><tr><th width="314">Event</th><th>Published</th></tr></thead><tbody><tr><td><strong>ApplicationInitialized</strong></td><td>After the application has been initialized</td></tr><tr><td><strong>CatalogSearching</strong></td><td>Before a search request is executed</td></tr><tr><td><strong>CatalogSearched</strong></td><td>After a search request has been executed</td></tr><tr><td><strong>CategoryTreeChanged</strong></td><td>An entity that affects the category tree display has changed</td></tr><tr><td><strong>CustomerAnonymized</strong></td><td>After a customer row has been anonymized by the GDPR tool</td></tr><tr><td><strong>CustomerRegistered</strong></td><td>After a user/customer has registered</td></tr><tr><td><strong>CustomerSignedIn</strong></td><td>After a user/customer has signed in</td></tr><tr><td><strong>GdprCustomerDataExported</strong></td><td>After a customer row has been exported by the GDPR tool</td></tr><tr><td><strong>ImageQueryCreated</strong></td><td>After an image query has been created and initialized by the media middleware with data from the current query string. Implies that a thumbnail is about to be created</td></tr><tr><td><strong>ImageProcessed</strong></td><td>After image processing has finished</td></tr><tr><td><strong>ImageProcessing</strong></td><td>Before image processing begins, but after the source has been loaded</td></tr><tr><td><strong>ImageUploaded</strong></td><td>After an image - that does NOT exceed maximum allowed size - has been uploaded. This gives subscribers the chance to still process the image, e.g. to achieve better compression before saving image data to storage. This event does NOT get published when the uploaded image is about to be processed anyway</td></tr><tr><td><strong>ImportBatchExecuted&#x3C;T></strong></td><td>After a batch of data of type T has been imported</td></tr><tr><td><strong>ImportExecuted</strong></td><td>After an import process has completed</td></tr><tr><td><strong>ImportExecuting</strong></td><td>Before an import process begins</td></tr><tr><td><strong>IndexingCompleted</strong></td><td>After an indexing process has completed</td></tr><tr><td><strong>IndexSegmentProcessed</strong></td><td>After an index segment (batch) has been processed</td></tr><tr><td><strong>MessageModelPartCreated&#x3C;T></strong></td><td>After the model part T for a mail message has been created</td></tr><tr><td><strong>MessageModelCreated</strong></td><td>After a mail message has been completely created</td></tr><tr><td><strong>MessageModelPartMapping</strong></td><td>When a system mapper cannot resolve a particular model type (e.g. a custom entity in a module)</td></tr><tr><td><strong>MessageQueuing</strong></td><td>Before a mail message is put to the send queue</td></tr><tr><td><strong>MenuBuilt</strong></td><td>After a UI menu has been built (but before being cached)</td></tr><tr><td><strong>MigrateShoppingCart</strong></td><td>After a shopping cart has been migrated</td></tr><tr><td><strong>ModelBound</strong></td><td>After a model has been bound</td></tr><tr><td><strong>NewsletterSubscribed</strong></td><td>After a user subscribed to a newsletter</td></tr><tr><td><strong>NewsletterUnsubscribed</strong></td><td>After a user unsubscribed from a newsletter</td></tr><tr><td><strong>OrderPaid</strong></td><td>After an order's status has changed to <em>Paid</em></td></tr><tr><td><strong>OrderPlaced</strong></td><td>After an order has been placed</td></tr><tr><td><strong>OrderUpdated</strong></td><td>After an order entity has been changed</td></tr><tr><td><strong>ProductCopied</strong></td><td>After a product has been copied/cloned</td></tr><tr><td><strong>RenderingOrderTotals</strong></td><td>Before rendering the order totals widget</td></tr><tr><td><strong>RowExporting</strong></td><td>Before exporting a data row, e.g. a product</td></tr><tr><td><strong>SeedingDbMigration</strong></td><td>Before seeding migration data</td></tr><tr><td><strong>TabStripCreated</strong></td><td>After a UI tab strip has been created</td></tr><tr><td><strong>ThemeSwitched</strong></td><td>After the main theme has been switched</td></tr><tr><td><strong>ValidatingCart</strong></td><td>Before validating the shopping cart</td></tr><tr><td><strong>ViewComponentExecuting&#x3C;T></strong></td><td>When a view component is about to create/prepare its model (of type T)</td></tr><tr><td><strong>ViewComponentResultExecuting</strong></td><td>When a view component is about to render the view</td></tr><tr><td><strong>ZoneRendering</strong></td><td>When a mail template zone is about to be rendered</td></tr></tbody></table>
