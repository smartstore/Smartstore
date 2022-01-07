using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Smartstore.Core.Localization.Routing
{
    public class LocalizedRoutingConvention : IApplicationModelConvention
    {
        private readonly IServiceProvider _serviceProvider;

        public LocalizedRoutingConvention(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                // POLICIES:
                // ===========================================
                // 1.) Allow: Relative ActionLocalizedRoute >>> DEL action route, INS absolutized action route
                // 2.) Allow: Absolute ActionLocalizedRoute >>> DEL action route, INS action route
                // 3.) Allow: ControllerRoute + Relative ActionLocalizedRoute >>> Combine & absolutize, DEL action route, INS action route
                // 4.) Allow: ControllerRoute + Absolute ActionLocalizedRoute >>> Combine, DEL action route, INS action route
                // 5.) Void: ControllerRoute + ActionRoute
                // ===========================================

                // iterate over action models
                foreach (var action in controller.Actions)
                {
                    var newSelectors = new List<SelectorModel>();
                    var deleteSelectors = new List<SelectorModel>();

                    foreach (var selector in action.Selectors)
                    {
                        var routeModel = selector.AttributeRouteModel;

                        if (routeModel?.Attribute is not ILocalizedRoute)
                        {
                            continue;
                        }

                        deleteSelectors.Add(selector);

                        newSelectors.AddRange(MakeLocalizedSelectors(controller, selector, true));
                        newSelectors.AddRange(MakeLocalizedSelectors(controller, selector, false));
                    }

                    deleteSelectors.Each(x => action.Selectors.Remove(x));
                    action.Selectors.AddRange(newSelectors);
                }
            }
        }

        private static IEnumerable<SelectorModel> MakeLocalizedSelectors(
            ControllerModel controllerModel, 
            SelectorModel actionSelector, 
            bool withCultureToken)
        {
            if (!actionSelector.AttributeRouteModel.IsAbsoluteTemplate && controllerModel.Selectors.Any())
            {
                // Combine all controller selectors with this relative action selector
                return controllerModel.Selectors.Select(x => MakeLocalizedSelector(x, actionSelector, withCultureToken));
            }

            return new[] { MakeLocalizedSelector(null, actionSelector, withCultureToken) };
        }

        private static SelectorModel MakeLocalizedSelector(
            SelectorModel controllerSelector,
            SelectorModel actionSelector, 
            bool withCultureToken)
        {
            var routeModel = controllerSelector != null
                ? AttributeRouteModel.CombineAttributeRouteModel(controllerSelector.AttributeRouteModel, actionSelector.AttributeRouteModel)
                : new AttributeRouteModel(actionSelector.AttributeRouteModel);

            var selector = new SelectorModel(actionSelector)
            {
                AttributeRouteModel = routeModel
            };

            if (withCultureToken)
            {
                routeModel.Template = AttributeRouteModel.CombineTemplates("{culture:culture}", routeModel.Template.TrimStart('~', '/'));
                routeModel.Order = -2;
                selector.AttributeRouteModel = routeModel;
            }
            else
            {
                routeModel.Order = -1;

                if (routeModel.Name.HasValue())
                {
                    routeModel.Name += "__noculture";
                }
            }

            if (!routeModel.IsAbsoluteTemplate)
            {
                // Absolutize pattern
                routeModel.Template = '/' + routeModel.Template;
            }

            selector.EndpointMetadata.Insert(0, new LocalizedRouteMetadata(selector.AttributeRouteModel, !withCultureToken));

            return selector;
        }
    }
}