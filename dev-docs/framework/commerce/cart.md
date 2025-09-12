# Cart

## Overview

The cart holds products a customer plans to buy. Each entry is a `ShoppingCartItem` that belongs to a `ShoppingCart` identified by customer, `ShoppingCartType` (`ShoppingCart` or `Wishlist`), and store. Items may carry attribute selections, bundle children, customer entered prices and other metadata.

The cart persists in the database and is cached per request. Services and controllers obtain it through `IShoppingCartService` which encapsulates all business rules such as maximum quantities, stock validation and access permissions.

## Getting a cart

Retrieve the current customer's cart for the active store:

```csharp
var cart = await _shoppingCartService.GetCartAsync(
    customer: Services.WorkContext.CurrentCustomer,
    cartType: ShoppingCartType.ShoppingCart,
    storeId: Services.StoreContext.CurrentStore.Id);
```

The optional `activeOnly` flag includes inactive items (e.g. when showing the full cart page). To quickly display the mini cart counter without loading the entire cart use `CountProductsInCartAsync`.

## Adding items

Create an `AddToCartContext` and call `AddToCartAsync` to let the service build and persist the necessary `ShoppingCartItem` records:

```csharp
var addToCartContext = new AddToCartContext
{
    Customer = Services.WorkContext.CurrentCustomer,
    Product = product,
    CartType = ShoppingCartType.ShoppingCart,
    Quantity = 1,
    AutomaticallyAddRequiredProducts = true
};
                    
var success = await _shoppingCartService.AddToCartAsync(addToCartContext);
```

If you already constructed child items (for example for a bundle) use `AddItemToCartAsync` to commit them. The service raises warnings for validation issues (out of stock, attribute mismatch, quantity limits) which callers may display.

## Updating or removing

Use `UpdateCartItemAsync` to change quantity or deactivate an item. `DeleteCartItemAsync` removes a single item including its children, while `DeleteCartAsync` clears an entire cart. All methods optionally reset checkout data to ensure consistency.

## Payment buttons

Payment providers with `PaymentMethodType.Button` can render express checkout buttons directly on the cart page. The view invokes the `CartPaymentButtons` partial which asks active providers for button components. These buttons usually skip the standard checkout and redirect straight to the provider. Before redirecting, they should call `SaveCartDataAsync` to persist checkout attributes and the reward-point flag. See the [payment provider guide](creating-a-payment-provider.md#payment-method-types) for details on implementing button payment methods.

## Migration and events

When an anonymous user signs in, the system migrates the temporary cart to the logged in user's cart by calling `MigrateCartAsync` and publishing a `MigrateShoppingCartEvent`. `ValidatingCartEvent` allows modules to intercept the cart before checkout and redirect the customer if necessary. During order summary generation `RenderingOrderTotalsEvent` offers another hook to augment totals.

## Related features

* The [rules engine](rules-engine.md) can target carts via `CartRuleProvider` to enable or disable discounts and other features.
* Wishlist functionality uses the same service by passing `ShoppingCartType.Wishlist`.
