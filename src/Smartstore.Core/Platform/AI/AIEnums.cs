namespace Smartstore.Core.AI
{
    [Flags]
    public enum AIProviderFeatures
    {
        None = 0,

        /// <summary>
        /// Generate new text content (e.g. product descriptions, articles).
        /// </summary>
        TextGeneration = 1 << 0,

        /// <summary>
        /// Translate text between different languages.
        /// </summary>
        Translation = 1 << 1,

        /// <summary>
        /// Generate images from text prompts.
        /// </summary>
        ImageGeneration = 1 << 2,

        /// <summary>
        /// Analyze and interpret images (vision capabilities).
        /// </summary>
        ImageAnalysis = 1 << 3,

        /// <summary>
        /// Generate theme variables.
        /// </summary>
        ThemeVarGeneration = 1 << 4,

        /// <summary>
        /// Provide general assistant functionality (QA, reasoning, planning).
        /// </summary>
        Assistance = 1 << 5
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