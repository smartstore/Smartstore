using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Identity
{
    public class DisplayExternalAuthWidgets : IActionFilter
    {
        private readonly ICommonServices _services;
        private readonly IWidgetProvider _widgetProvider;
        private readonly IProviderManager _providerManager;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;

        public DisplayExternalAuthWidgets(
            ICommonServices services,
            IWidgetProvider widgetProvider,
            IProviderManager providerManager,
            ExternalAuthenticationSettings externalAuthenticationSettings)
        {
            _services = services;
            _widgetProvider = widgetProvider;
            _providerManager = providerManager;
            _externalAuthenticationSettings = externalAuthenticationSettings;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var storeId = _services.StoreContext.CurrentStore.Id;
            var authMethods = _providerManager.GetAllProviders<IExternalAuthenticationMethod>(storeId);

            foreach (var authMethod in authMethods)
            {
                if (authMethod.IsMethodActive(_externalAuthenticationSettings))
                {
                    var widget = authMethod.Value.GetDisplayWidget(storeId);
                    if (widget != null)
                    {
                        _widgetProvider.RegisterWidget("external_auth_buttons", widget);
                    }
                }
            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }
    }
}