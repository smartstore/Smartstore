namespace Smartstore.Core.AI
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
    /// Represents the topic of an AI chat.
    /// </summary>
    public enum AIChatTopic
    {
        /// <summary>
        /// Chat to generate simple text.
        /// </summary>
        Text,

        /// <summary>
        /// Chat to generate rich text.
        /// </summary>
        RichText,

        /// <summary>
        /// Chat to translate text.
        /// </summary>
        Translation,

        /// <summary>
        /// Chat to generate suggestions.
        /// </summary>
        Suggestion,

        /// <summary>
        /// Chat to generate images.
        /// </summary>
        Image
    }

    /// <summary>
    /// Represents the AI image creation type.
    /// </summary>
    public enum AIImageFormat
    {
        Horizontal,
        Vertical,
        Square
    }

    /// <summary>
    /// Represents the AI role.
    /// </summary>
    public enum AIRole
    {
        Translator,
        Copywriter,
        Marketer,
        SEOExpert,
        Blogger,
        Journalist,
        SalesPerson,
        ProductExpert,
        HtmlEditor,
        ImageAnalyzer
    }
}