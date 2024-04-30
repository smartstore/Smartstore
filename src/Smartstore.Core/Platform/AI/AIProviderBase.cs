using Smartstore.Core.Localization;
using Smartstore.Http;

namespace Smartstore.Core.Platform.AI
{
    [Flags]
    public enum AIProviderFeatures
    {
        None = 0,
        TextCreation = 1 << 0,
        TextTranslation = 1 << 1,
        ImageCreation = 1 << 2,
        ImageAnalysis = 1 << 3,
        ThemeVarCreation = 1 << 4,
        Assistence = 1 << 5
    }

    /// <summary>
    /// Base class to implement AI providers.
    /// </summary>
    public abstract class AIProviderBase : IAIProvider
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

        #region Feature flags

        // TODO: (mh) No interface, just AIProvider.
        // TODO: (mh) This belongs to IAIProvider. All methods starting with "Supports*" must be removed from interface. TBD with MC.
        // TODO: (mh) Method naming convention: CanDoSomething, e.g. CanCreateText, CanCreateImage, CanTranslate etc.
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

        // TODO: (mh) Rename --> GetDialogRoute
        /// <inheritdoc/>
        public abstract RouteInfo GetModalDialogRoute(AIModalDialogType modalDialogType);
    }
}
