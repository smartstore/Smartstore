using Smartstore.Core.Localization;
using Smartstore.Http;

namespace Smartstore.Core.Platform.AI
{
    /// <summary>
    /// Base class to implement AI providers.
    /// </summary>
    public abstract class AIProviderBase : IAIProvider
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

        #region Feature flags

        // TODO: (mh) (ai) No interface, just AIProvider.
        // TODO: (mh) (ai) This belongs to IAIProvider. All methods starting with "Supports*" must be removed from interface. TBD with MC.
        // TODO: (mh) (ai) Method naming convention: CanDoSomething, e.g. CanCreateText, CanCreateImage, CanTranslate etc.
        public virtual AIProviderFeatures Features { get; } = AIProviderFeatures.None;

        /// <inheritdoc/>
        public bool SupportsTextCreation
        {
            get => Features.HasFlag(AIProviderFeatures.TextCreation);
        }

        /// <inheritdoc/>
        public bool SupportsTextTranslation
        {
            get => Features.HasFlag(AIProviderFeatures.TextTranslation);
        }

        /// <inheritdoc/>
        public bool SupportsImageCreation
        {
            get => Features.HasFlag(AIProviderFeatures.ImageCreation);
        }

        /// <inheritdoc/>
        public bool SupportsImageAnalysis
        {
            get => Features.HasFlag(AIProviderFeatures.ImageAnalysis);
        }
        
        /// <inheritdoc/>
        public bool SuportsThemeVarCreation
        {
            get => Features.HasFlag(AIProviderFeatures.ThemeVarCreation);
        }

        /// <inheritdoc/>
        public bool SupportsAssistence
        {
            get => Features.HasFlag(AIProviderFeatures.Assistence);
        }

        #endregion

        public bool Supports(AIProviderFeatures feature)
            => Features.HasFlag(feature);

        public abstract RouteInfo GetDialogRoute(AIDialogType modalDialogType);
    }
}
