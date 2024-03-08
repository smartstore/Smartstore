namespace Smartstore.Core.Checkout.Orders.Handlers
{
    public class ConfirmHandler : CheckoutHandlerBase
    {
        protected override string Action => "Confirm";

        public override int Order => int.MaxValue;

        public override Task<CheckoutHandlerResult> ProcessAsync(CheckoutContext context)
            => Task.FromResult(new CheckoutHandlerResult(true));
    }
}
