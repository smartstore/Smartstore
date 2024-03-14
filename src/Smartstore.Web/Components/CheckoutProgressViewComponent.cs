using System.Collections.Frozen;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Web.Models.Checkout;

namespace Smartstore.Web.Components
{
    public class CheckoutProgressViewComponent(ICheckoutFactory checkoutFactory) : SmartViewComponent
    {
        private static readonly FrozenDictionary<string, string> _knownLabelKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { CheckoutActionNames.BillingAddress, "Checkout.Progress.Address" },
            { CheckoutActionNames.ShippingAddress, null },
            { CheckoutActionNames.ShippingMethod, "Checkout.Progress.Shipping" },
            { CheckoutActionNames.PaymentMethod, "Checkout.Progress.Payment" },
            { CheckoutActionNames.Confirm, "Checkout.Progress.Confirm" }
        }
        .ToFrozenDictionary();

        private readonly ICheckoutFactory _checkoutFactory = checkoutFactory;

        public IViewComponentResult Invoke(string action, string controller = "Checkout", string area = null)
        {
            var isCartPage = action.EqualsNoCase(nameof(ShoppingCartController.Cart)) && controller.EqualsNoCase("ShoppingCart");
            var steps = _checkoutFactory.GetCheckoutSteps();
            var currentStep = _checkoutFactory.GetCheckoutStep(action, controller, area);
            var currentMetadata = currentStep?.Handler?.Metadata;
            var currentOrdinal = 0;

            if (action.EqualsNoCase(CheckoutActionNames.Completed))
            {
                currentOrdinal = int.MaxValue;
            }
            else if (currentMetadata != null)
            {
                var labelKey = currentMetadata.ProgressLabelKey ?? _knownLabelKeys.Get(currentMetadata.DefaultAction);
                if (labelKey.IsEmpty())
                {
                    // Progress step is not displayed -> need to fix "currentOrdinal".
                    currentMetadata = _checkoutFactory.GetNextCheckoutStep(currentStep, false)?.Handler?.Metadata;
                }

                currentOrdinal = currentMetadata?.Order ?? 0;
            }

            var models = steps
                .Select(x =>
                {
                    var md = x.Handler.Metadata;
                    var labelKey = md.ProgressLabelKey ?? _knownLabelKeys.Get(md.DefaultAction);

                    if (labelKey.IsEmpty())
                    {
                        // Skip handler. Do not show progress step.
                        return null;
                    }

                    return new CheckoutProgressStepModel
                    {
                        Name = md.DefaultAction.ToLowerInvariant(),
                        Url = Url.Action(md.DefaultAction, md.Controller, md.Area.HasValue() ? new { area = md.Area } : null),
                        Label = T(labelKey),
                        Active = currentMetadata?.HandlerType?.Equals(md.HandlerType) ?? false,
                        Visited = md.Order < currentOrdinal
                    };
                })
                .Where(x => x != null)
                .ToList();

            models.Insert(0, new()
            {
                Name = "cart",
                Url = Url.RouteUrl("ShoppingCart"),
                Label = T("Checkout.Progress.Cart"),
                Active = isCartPage,
                Visited = !isCartPage
            });

            models.Add(new()
            {
                Name = "complete",
                Url = "javascript:;",
                Label = T("Checkout.Progress.Complete"),
                Active = action.EqualsNoCase(CheckoutActionNames.Completed) && controller.EqualsNoCase("Checkout"),
                Visited = false
            });

            return View(models);
        }
    }
}
