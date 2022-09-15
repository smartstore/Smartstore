using Microsoft.OData.ModelBuilder;

namespace Smartstore.Web.Api
{
    /// <summary>
    /// Represents a provider to build data models for the OData API service.
    /// </summary>
    public interface IODataModelProvider
    {
        /// <summary>
        /// Builds the Entity Data Model (EDM) for the OData API service.
        /// Allows to add and configure entity sets to the model.
        /// </summary>
        void Build(ODataModelBuilder builder, int version);
    }
}
