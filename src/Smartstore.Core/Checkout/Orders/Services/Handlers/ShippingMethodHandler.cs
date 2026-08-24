using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Core.Checkout.Orders.Handlers;

[CheckoutStep(30, CheckoutActionNames.ShippingMethod)]
public class ShippingMethodHandler : CheckoutHandlerBase
{
    private readonly IShippingService _shippingService;
    private readonly ShippingSettings _shippingSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    public ShippingMethodHandler(
        IShippingService shippingService,
        ShippingSettings shippingSettings,
        ShoppingCartSettings shoppingCartSettings)
    {
        _shippingService = shippingService;
        _shippingSettings = shippingSettings;
        _shoppingCartSettings = shoppingCartSettings;
    }

    public override async Task<CheckoutResult> RefreshAsync(CheckoutContext context)
    {
        if (context.Part == null)
        {
            return new(false);
        }

        var result = await TrySaveShippingOption(context) ?? new(false);
        if (!result.Success)
        {
            return result;
        }



        return new(false);
    }

    public override async Task<CheckoutResult> ProcessAsync(CheckoutContext context)
    {
        var cart = context.Cart;
        var ga = cart.Customer.GenericAttributes;
        var options = ga.OfferedShippingOptions;
        CheckoutError[] errors = null;
        var saveAttributes = false;

        if (!cart.IsShippingRequired)
        {
            if (ga.SelectedShippingOption != null || ga.OfferedShippingOptions != null)
            {
                ga.SelectedShippingOption = null;
                ga.OfferedShippingOptions = null;
                await ga.SaveChangesAsync();
            }

            return new(true, null, true);
        }

        var result = await TrySaveShippingOption(context);
        if (result != null)
        {
            return result;
        }

        if (options.IsNullOrEmpty())
        {
            (options, errors) = await GetShippingOptions(context);
            if (options.Count == 0)
            {
                return new(false, errors);
            }

            // Performance optimization. Cache returned shipping options.
            // We will use them later (after a customer has selected an option).
            ga.OfferedShippingOptions = options;
            saveAttributes = true;
        }

        var skip = _shippingSettings.SkipShippingIfSingleOption && options.Count == 1;
        if (skip)
        {
            ga.SelectedShippingOption = options[0];
            saveAttributes = true;
        }

        if (_shoppingCartSettings.QuickCheckoutEnabled && ga.SelectedShippingOption == null)
        {
            var preferredOption = ga.PreferredShippingOption;
            if (preferredOption != null && preferredOption.ShippingMethodId != 0)
            {
                if (preferredOption.ShippingRateComputationMethodSystemName.HasValue())
                {
                    ga.SelectedShippingOption = options.FirstOrDefault(x => x.ShippingMethodId == preferredOption.ShippingMethodId &&
                        x.ShippingRateComputationMethodSystemName.EqualsNoCase(preferredOption.ShippingRateComputationMethodSystemName));
                }

                ga.SelectedShippingOption ??= options
                    .Where(x => x.ShippingMethodId == preferredOption.ShippingMethodId)
                    .OrderBy(x => x.Rate)
                    .FirstOrDefault();
            }

            saveAttributes = ga.SelectedShippingOption != null;
        }

        if (saveAttributes)
        {
            await ga.SaveChangesAsync();
        }

        return new(ga.SelectedShippingOption != null, errors, skip);
    }

    private async Task<CheckoutResult> TrySaveShippingOption(CheckoutContext context)
    {
        if (!context.IsCurrentRoute(HttpMethods.Post, CheckoutActionNames.ShippingMethod))
        {
            return null;
        }

        string shippingOption = null;
        if (context.Model is string model)
        {
            shippingOption = model;
        }
        else if (context.Part == CheckoutPart.OrderTotals
            && context.HttpContext.Request.Form.TryGetValue("shippingoption", out var val))
        {
            shippingOption = val.ToString();
        }

        var splittedOption = shippingOption.SplitSafe("___").ToArray();
        if (splittedOption.Length != 2)
        {
            return new(false);
        }

        CheckoutError[] errors = null;
        var selectedId = splittedOption[0].ToInt();
        var providerSystemName = splittedOption[1];
        var ga = context.Cart.Customer.GenericAttributes;
        var options = ga.OfferedShippingOptions;

        if (options.IsNullOrEmpty())
        {
            // Shipping option was not found in customer attributes. Load via shipping service.
            (options, errors) = await GetShippingOptions(context, providerSystemName);
        }
        else
        {
            // Loaded cached results. Filter result by a chosen shipping rate computation method.
            options = options.Where(x => x.ShippingRateComputationMethodSystemName.EqualsNoCase(providerSystemName)).ToList();
        }

        var selectedOption = options.FirstOrDefault(x => x.ShippingMethodId == selectedId);
        if (selectedOption != null)
        {
            ga.SelectedShippingOption = selectedOption;
            ga.PreferredShippingOption = selectedOption;

            await ga.SaveChangesAsync();
        }

        return new(selectedOption != null, errors);
    }

    private async Task<(List<ShippingOption> Options, CheckoutError[] Errors)> GetShippingOptions(CheckoutContext context, string providerSystemName = null)
    {
        CheckoutError[] errors = null;
        var response = await _shippingService.GetShippingOptionsAsync(context.Cart, context.Cart.Customer.ShippingAddress, providerSystemName, context.Cart.StoreId);

        if (response.ShippingOptions.Count == 0 && context.IsCurrentRoute(HttpMethods.Get, CheckoutActionNames.ShippingMethod))
        {
            errors = response.Errors
                .Select(x => new CheckoutError(string.Empty, x))
                .ToArray();
        }

        return (response.ShippingOptions, errors);
    }
}