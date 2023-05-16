using Microsoft.AspNetCore.Mvc;
using Smartstore.Core;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public abstract class PayPalViewComponentBase : SmartViewComponent
    {
        private PayPalSettings _settings;
        private string _routeIdent;

        protected PayPalSettings Settings
        {
            get => _settings ??= Services.Resolve<PayPalSettings>();
        }

        protected string RouteIdent
        {
            get => _routeIdent ??= Request.RouteValues.GenerateRouteIdentifier();
        }

        /// <summary>
        /// Renders PayPal buttons widget.
        /// </summary>
        public Task<IViewComponentResult> InvokeAsync()
        {
            // If client id or secret haven't been configured yet, don't render buttons.
            if (!Settings.ClientId.HasValue() || !Settings.Secret.HasValue())
            {
                return Task.FromResult(Empty());
            }

            return InvokeCoreAsync();
        }

        protected virtual Task<IViewComponentResult> InvokeCoreAsync()
            => Task.FromResult(InvokeCore());

        protected abstract IViewComponentResult InvokeCore();
    }
}