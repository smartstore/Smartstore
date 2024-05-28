namespace Smartstore.Core.Platform.AI
{
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