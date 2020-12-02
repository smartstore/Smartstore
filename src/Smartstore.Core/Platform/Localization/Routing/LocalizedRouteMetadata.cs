using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Smartstore.Core.Localization.Routing
{
    public sealed class LocalizedRouteMetadata
    {
        public LocalizedRouteMetadata(AttributeRouteModel model)
        {
            AttributeRouteModel = model;
        }

        public AttributeRouteModel AttributeRouteModel { get; init; }
    }
}