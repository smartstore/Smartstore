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
        /// <param name="builder">Model builder to map CLR classes to an Entity Data Model (EDM).</param>
        /// <param name="version">The API version for which the model should be built. At the moment always 1.</param>
        void Build(ODataModelBuilder builder, int version);
    }
}
