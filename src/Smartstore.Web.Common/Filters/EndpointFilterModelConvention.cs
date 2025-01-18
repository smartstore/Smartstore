using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Smartstore.Web.Filters
{
    public class EndpointFilterModelConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            var filters = application.Filters.OfType<EndpointFilterMetadata>().ToArray();

            foreach (var filter in filters)
            {
                application.Filters.Remove(filter);

                var isControllerFilter = filter.ActionMethods.IsNullOrEmpty();
                var actualFilter = filter.GetFilter();

                foreach (var controller in application.Controllers)
                {
                    if (filter.ControllerType.IsAssignableFrom(controller.ControllerType))
                    {
                        if (isControllerFilter)
                        {
                            controller.Filters.Add(actualFilter);
                        }
                        else
                        {
                            foreach (var action in controller.Actions)
                            {
                                if (filter.ActionMethods.Contains(action.ActionMethod))
                                {
                                    action.Filters.Add(actualFilter);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
