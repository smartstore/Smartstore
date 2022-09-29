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

        /// <summary>
        /// Gets a stream of XML formatted source code comments.
        /// </summary>
        /// <remarks>
        /// To create the XML comments file, enable "Documentation file" in project properties and leave file path empty.
        /// Optionally append 1591 to "Suppress specific warnings" to suppress warning about missing XML comments.
        /// </remarks>
        /// <param name="appContext">Application context.</param>
        /// <returns>Stream of XML formatted source code comments. <c>null</c> if no source comments exist.</returns>
        Stream GetXmlCommentsStream(IApplicationContext appContext);
    }
}
