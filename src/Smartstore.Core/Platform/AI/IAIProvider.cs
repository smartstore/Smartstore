using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Core.Platform.AI
{
    public partial interface IAIProvider : IProvider
    {
        // TODO: (mh) 
        
        /// <summary>
        /// Defines whether the provider can create text.
        /// </summary>
        bool SupportsTextCreation { get; }

        /// <summary>
        /// Defines whether the provider can translate text.
        /// </summary>
        bool SupportsTextTranslation { get; }

        /// <summary>
        /// Defines whether the provider can create images.
        /// </summary>
        bool SupportsImageCreation { get; }

        /// <summary>
        /// Defines whether the provider can analyse images.
        /// </summary>
        bool SupportsImageAnalysis { get; }

        /// <summary>
        /// Defines whether the provider can create theme vars.
        /// </summary>
        bool SuportsThemeVarCreation { get; }

        /// <summary>
        /// Defines whether the provider provides an assistent.
        /// </summary>
        bool SupportsAssistence { get; }

        // TODO: (mh) Rename --> GetDialogRoute
        /// <summary>
        /// Gets a route for the given modal dialog type.
        /// </summary>
        /// <returns>RouteInfo for the modal dialog.</returns>
        RouteInfo GetModalDialogRoute(AIModalDialogType modalDialogType);
    }
}