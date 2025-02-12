using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Smartstore.Web.Filters
{
    /// <summary>
    /// A convention that applies endpoint filters to controllers and actions based on specified criteria.
    /// </summary>
    public class EndpointFilterModelConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            // Retrieve all endpoint filters from the application model
            var endpointFilters = application.Filters.OfType<EndpointFilterMetadata>().ToArray();

            foreach (var endpointFilter in endpointFilters)
            {
                // Remove the endpoint filter from the application filters
                application.Filters.Remove(endpointFilter);

                // Determine if the filter is a controller filter
                var isControllerFilter = endpointFilter.IsControllerFilter();
                // Get the actual filter to be applied
                var actualFilter = endpointFilter.GetFilter();

                // Iterate through each controller in the application
                foreach (var controller in application.Controllers)
                {
                    // Check if the endpoint filter matches the controller
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
                                // Check if the endpoint filter matches the action
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
