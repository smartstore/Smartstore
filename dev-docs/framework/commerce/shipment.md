# Shipment

## Overview

Shipping covers the calculation of shipping rates before checkout and the handling of physical shipments after an order is placed. `IShippingService` orchestrates shipping rate computation providers that implement `IShippingRateComputationMethod`. Providers can return a `ShipmentTracker` to expose tracking links. Shipments themselves are attached to orders and contain a collection of `ShipmentItem` rows that specify which order items and quantities are dispatched.

## Loading shipping options

To present the customer with possible shipping methods, create a `ShippingOptionRequest` from the cart and shipping address and then resolve rates through `GetShippingOptionsAsync`:

```csharp
var request = await _shippingService.CreateShippingOptionRequestAsync(
    cart,
    address,
    storeId: Services.StoreContext.CurrentStore.Id);

var response = await _shippingService.GetShippingOptionsAsync(request);
foreach (var option in response.ShippingOptions)
{
    Console.WriteLine($"{option.Name}: {option.Rate}");
}
```

`ShippingOptionResponse` may carry warnings when a provider cannot return rates. Use `LoadEnabledShippingProviders` to enumerate providers and `GetAllShippingMethodsAsync` to list configured methods for administration screens.

## Implementing a shipping rate provider

Custom shipping logic lives in modules that register a class implementing `IShippingRateComputationMethod`:

```csharp
public class FixedRateProvider : IShippingRateComputationMethod
{
    public ShippingRateComputationMethodType ShippingRateComputationMethodType
        => ShippingRateComputationMethodType.Offline;

    public Task<ShippingOptionResponse> GetShippingOptionsAsync(ShippingOptionRequest request)
        => Task.FromResult(new ShippingOptionResponse
        {
            ShippingOptions =
            {
                new ShippingOption { Name = "Fixed", Rate = Money.FromDecimal(5m) }
            }
        });

    public IShipmentTracker ShipmentTracker => null;
}
```

Register the provider in the module’s `Startup` and package it like the implementation in `src/Smartstore.Modules/Smartstore.Shipping`.

## Working with shipments

After payment, orders can be split into one or more shipments. Use `IOrderProcessingService` to create and update them:

```csharp
var shipment = await _orderProcessingService.AddShipmentAsync(order, new[]
{
    new ShipmentItem(orderItem, quantity: 1)
});

await _orderProcessingService.ShipAsync(shipment, notifyCustomer: true);
```

`ShipAsync` and `DeliverAsync` update the order’s shipping status and dispatch notification messages. `CanAddItemsToShipmentAsync` helps determine whether more items remain to be shipped.
