namespace Smartstore.Core.Checkout.Orders;

public abstract class CheckoutHandlerBase : ICheckoutHandler
{
    public abstract Task<CheckoutResult> ProcessAsync(CheckoutContext context);

    public virtual Task<CheckoutResult> RefreshAsync(CheckoutContext context)
        => Task.FromResult(new CheckoutResult(false));
}
