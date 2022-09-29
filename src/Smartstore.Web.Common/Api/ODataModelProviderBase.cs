using Microsoft.OData.ModelBuilder;

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
        /// <param name="installedOnly">Consider installed/loaded modules only.</param>
        /// <returns>Stream of XML formatted source code comments. <c>null</c> if no source comments exist.</returns>
        protected virtual Stream GetDefaultXmlCommentsStream(IApplicationContext appContext, string systemName, bool installedOnly = true)
        {
            Guard.NotNull(appContext, nameof(appContext));
            Guard.NotEmpty(systemName, nameof(systemName));

            var descriptor = appContext.ModuleCatalog.GetModuleByName(systemName, installedOnly);
            if (descriptor != null)
            {
                var subpath = Path.Combine(systemName, Path.ChangeExtension(descriptor.AssemblyName, "xml"));
                var xmlFile = appContext.ModulesRoot.GetFile(subpath);
                if (xmlFile.Exists)
                {
                    return xmlFile.OpenRead();
                }
            }

            return null;
        }
    }
}
