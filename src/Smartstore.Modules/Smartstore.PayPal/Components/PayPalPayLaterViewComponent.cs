using Microsoft.AspNetCore.Mvc;
using Smartstore.Core;
using Smartstore.Web.Components;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.PayPal.Components
{
    /// <summary>
    /// Renders the pay later widget on the product detail page.
    /// </summary>
    public class PayPalPayLaterViewComponent : SmartViewComponent
    {
        private readonly ICommonServices _services;
        private readonly PayPalSettings _settings;

        public PayPalPayLaterViewComponent(ICommonServices services, PayPalSettings settings)
        {
            _services = services;
            _settings = settings;
        }

        public IViewComponentResult Invoke(object model)
        {
            // If client id or secret haven't been configured yet, don't render button.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }

            var productDetailsModel = (ProductDetailsModel)model;

            // PayPal allows pay later only for amount between 99€ und 5.000€.
            if (productDetailsModel != null && productDetailsModel.Price.FinalPrice.Amount >= 99 && productDetailsModel.Price.FinalPrice.Amount <= 5000)
            {
                var scriptUrl = $"https://www.paypal.com/sdk/js" +
                    $"?client-id={_settings.ClientId}" +
                    $"&currency={_services.WorkContext.WorkingCurrency.CurrencyCode}" +
                    // Ensures no breaking changes will be applied in SDK.
                    $"&integration-date=2021-12-14" +
                    $"&components=messages";

                ViewBag.ScriptUrl = scriptUrl;
                ViewBag.Price = productDetailsModel.Price.FinalPrice.Amount.ToStringInvariant("F");
                return View();
            }

            return Empty();
        }
    }
}