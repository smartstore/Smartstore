using Smartstore.Core.AI.Prompting;
using Smartstore.Core.Localization;

namespace Smartstore.Core.AI
{
    /// <summary>
    /// A base class to implement <see cref="IAIProvider"/>.
    /// </summary>
    public abstract class AIProviderBase : IAIProvider
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

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

        public virtual string[] GetPreferredModelNames(AIChatTopic topic)
            => null;

        public virtual string[] GetDefaultModelNames()
            => ["default"];

        public virtual Task<string> ChatAsync(AIChat chat, CancellationToken cancelToken = default)
            => throw new NotImplementedException();

        public virtual IAsyncEnumerable<AIChatCompletionResponse> ChatAsStreamAsync(
            AIChat chat,
            int numAnswers,
            CancellationToken cancelToken = default)
            => throw new NotImplementedException();

        public virtual Task<string[]> CreateImagesAsync(IAIImageModel prompt, int numImages = 1, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        public virtual Task<string> AnalyzeImageAsync(string url, string prompt, CancellationToken cancelToken = default)
            => throw new NotSupportedException();
    }
}
