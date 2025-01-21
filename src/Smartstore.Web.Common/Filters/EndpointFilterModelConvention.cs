using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Smartstore.Web.Filters
{
    public class EndpointFilterModelConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            var endpointFilters = application.Filters.OfType<EndpointFilterMetadata>().ToArray();

            foreach (var endpointFilter in endpointFilters)
            {
                application.Filters.Remove(endpointFilter);

                var isControllerFilter = endpointFilter.IsControllerFilter();
                var actualFilter = endpointFilter.GetFilter();

                foreach (var controller in application.Controllers)
                {
                    if (endpointFilter.MatchController(controller))
                    {
                        if (isControllerFilter)
                        {
                            controller.Filters.Add(actualFilter);
                        }
                        else
                        {
                            foreach (var action in controller.Actions)
                            {
                                if (endpointFilter.MatchAction(action))
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
