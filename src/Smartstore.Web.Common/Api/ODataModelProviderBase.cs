using Microsoft.OData.ModelBuilder;
using Smartstore.IO;

namespace Smartstore.Web.Api
{
    /// <summary>
    /// Base class for OData model providers.
    /// </summary>
    public abstract class ODataModelProviderBase : IODataModelProvider
    {
        /// <inheritdoc/>
        public abstract void Build(ODataModelBuilder builder, int version);

        /// <inheritdoc/>
        public virtual Stream GetXmlCommentsStream(IApplicationContext appContext)
            => null;

        /// <summary>
        /// Gets the stream of the default XML source code comments file.
        /// </summary>
        /// <param name="appContext">Application context.</param>
        /// <param name="systemName">Module systemname.</param>
        /// <returns>Stream of XML formatted source code comments. <c>null</c> if no source comments exist.</returns>
        protected virtual Stream GetModuleXmlCommentsStream(IApplicationContext appContext, string systemName)
        {
            Guard.NotNull(appContext, nameof(appContext));
            Guard.NotEmpty(systemName, nameof(systemName));

            var descriptor = appContext.ModuleCatalog.GetModuleByName(systemName, true);
            if (descriptor != null)
            {
                var xmlFilePath = Path.ChangeExtension(descriptor.AssemblyName, "xml");
                var xmlFile = descriptor.ContentRoot.GetFile(xmlFilePath);
                if (xmlFile.Exists)
                {
                    return xmlFile.OpenRead();
                }
            }

            return null;
        }
    }
}
