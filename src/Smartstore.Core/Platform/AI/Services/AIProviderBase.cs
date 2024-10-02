using Smartstore.Core.Platform.AI.Prompting;
using Smartstore.Http;

namespace Smartstore.Core.Platform.AI
{
    /// <summary>
    /// A base class to implement <see cref="IAIProvider"/>.
    /// </summary>
    public abstract class AIProviderBase : IAIProvider
    {
        public abstract bool IsActive();

        public abstract bool Supports(AIProviderFeatures feature);

        public bool SupportsTextCreation
            => Supports(AIProviderFeatures.TextCreation);

        public bool SupportsTextTranslation
            => Supports(AIProviderFeatures.TextTranslation);

        public bool SupportsImageCreation
            => Supports(AIProviderFeatures.ImageCreation);

        public bool SupportsImageAnalysis
            => Supports(AIProviderFeatures.ImageAnalysis);

        public bool SuportsThemeVarCreation
            => Supports(AIProviderFeatures.ThemeVarCreation);

        public bool SupportsAssistence
            => Supports(AIProviderFeatures.Assistence);

        public abstract RouteInfo GetDialogRoute(AIChatTopic topic);

        // TODO: (mg) (ai) Maybe better to return ["default"] ?
        public virtual string[] GetPreferredTextModelNames() => [];

        // TODO: (mg) (ai) Maybe better to return ["default"] ?
        public virtual string[] GetPreferredImageModelNames() => [];

        public virtual Task<string> ChatAsync(AIChat chat, string modelName = null, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        public virtual IAsyncEnumerable<string> ChatAsStreamAsync(AIChat chat, string modelName = null, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        public virtual Task<string[]> CreateImagesAsync(IAIImageModel prompt, int numImages = 1, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        public virtual Task<string> AnalyzeImageAsync(string url, string prompt, CancellationToken cancelToken = default)
            => throw new NotSupportedException();
    }
}
