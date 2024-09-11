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
    /// Represents the AI modal dialog type.
    /// </summary>
    public enum AIDialogType
    {
        /// <summary>
        /// Used to open a dialog for image creation.
        /// </summary>
        Image,

        /// <summary>
        /// Used to open a dialog for simple text creation.
        /// </summary>
        Text,

        /// <summary>
        /// Used to open a dialog for rich text creation.
        /// </summary>
        RichText,

        /// <summary>
        /// Used to open a dialog for translations.
        /// </summary>
        Translation,

        /// <summary>
        /// Used to open a dialog for suggestions.
        /// </summary>
        Suggestion
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

    public enum ImageCreationStyle
    {
        Vivid,
        Natural
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
        ProductExpert
    }
}