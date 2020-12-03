using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Smartstore.Core.Localization.Routing
{
    public sealed class LocalizedRouteMetadata
    {
        public LocalizedRouteMetadata(AttributeRouteModel model, bool isCultureNeutralRoute)
        {
            AttributeRouteModel = model;
            IsCultureNeutralRoute = isCultureNeutralRoute;
        }

        public AttributeRouteModel AttributeRouteModel { get; init; }
        public bool IsCultureNeutralRoute { get; init; }
    }
}