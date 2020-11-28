using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                // ...

                // iterate over action models
                foreach (var action in controller.Actions)
                {
                    var newSelectors = new List<SelectorModel>(2);
                    
                    foreach (var selector in action.Selectors)
                    {
                        var routeModel = selector.AttributeRouteModel;

                        if (routeModel == null)
                        {
                            continue;
                        }
                        
                        var cultureAwareSelector = new SelectorModel(selector) 
                        {
                            AttributeRouteModel = new AttributeRouteModel(routeModel)
                        };

                        cultureAwareSelector.AttributeRouteModel.Template = "/{culture}" + routeModel.Template;
                        newSelectors.Add(cultureAwareSelector);
                    }

                    newSelectors.Each(x => action.Selectors.Insert(0, x));
                }
            }
        }
    }
}
