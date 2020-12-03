using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;

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
                // ...

                // iterate over action models
                foreach (var action in controller.Actions)
                {
                    var newSelectors = new List<SelectorModel>(2);
                    var deleteSelectors = new List<SelectorModel>();

                    foreach (var selector in action.Selectors)
                    {
                        //selector.ActionConstraints.Insert(0, new CultureActionConstraint());
                        
                        var routeModel = selector.AttributeRouteModel;
                        if (routeModel?.Attribute is not ILocalizedRoute)
                        {
                            continue;
                        }

                        routeModel.Order = -1;

                        deleteSelectors.Add(selector);

                        var cultureAwareSelector = new SelectorModel(selector)
                        {
                            AttributeRouteModel = new AttributeRouteModel(routeModel)
                        };
                        //cultureAwareSelector.ActionConstraints.Insert(0, new CultureActionConstraint());
                        cultureAwareSelector.AttributeRouteModel.Order = -2;
                        cultureAwareSelector.AttributeRouteModel.Template = "/{culture:culture}" + routeModel.Template;

                        cultureAwareSelector.EndpointMetadata.Add(new LocalizedRouteMetadata(cultureAwareSelector.AttributeRouteModel, false));
                        selector.EndpointMetadata.Add(new LocalizedRouteMetadata(selector.AttributeRouteModel, true));

                        newSelectors.Add(cultureAwareSelector);
                        newSelectors.Add(new SelectorModel(selector));
                    }


                    deleteSelectors.Each(x => action.Selectors.Remove(x));
                    action.Selectors.AddRange(newSelectors);
                    //newSelectors.Each(x => action.Selectors.Insert(0, x));
                }
            }
        }
    }
}
