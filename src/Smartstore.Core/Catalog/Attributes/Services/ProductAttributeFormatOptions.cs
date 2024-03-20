#nullable enable

namespace Smartstore.Core.Catalog.Attributes
{
    public class ProductAttributeFormatOptions
    {
        const string DefaultFormatTemplate = "{0}: {1}";
        const string DefaultPriceFormatTemplate = " ({0})";
        const string DefaultItemSeparator = "<br />";
        const string DefaultOptionsSeparator = ", ";

        public static ProductAttributeFormatOptions Default { get; } = new();
        public static ProductAttributeFormatOptions PlainText { get; } = new()
        {
            ItemSeparator = ", ", 
            IncludePrices = false, 
            IncludeHyperlinks = false, 
            IncludeGiftCardAttributes = false
        };

        /// <summary>
        /// Format template to be used for each attribute.
        /// Placeholder {0}: attribute name, placeholder {1}: attribute value. 
        /// Default: "{0}: {1}".
        /// </summary>
        public string FormatTemplate { get; init; } = DefaultFormatTemplate;

        /// <summary>
        /// Format template to be used for the attribute price adjustment to be appended to the attribute value (if applicable).
        /// Placeholder {0}: price. Default: " ({0})".
        /// </summary>
        public string PriceFormatTemplate { get; init; } = DefaultPriceFormatTemplate;

        /// <summary>
        /// Separator between each formatted attribute. Default: "<c>&lt;br /&gt;</c>".
        /// </summary>
        public string? ItemSeparator { get; init; } = DefaultItemSeparator;

        /// <summary>
        /// Separator between grouped attribute options. Default: ", ".
        /// <c>null</c> to not group at all.
        /// </summary>
        public string? OptionsSeparator { get; init; } = DefaultOptionsSeparator;

        /// <summary>
        /// A value indicating whether to HTML encode values. Default = true.
        /// </summary>
        public bool HtmlEncode { get; init; } = true;

        /// <summary>
        /// A value indicating whether to include prices. Default = true.
        /// </summary>
        public bool IncludePrices { get; init; } = true;

        /// <summary>
        /// A value indicating whether to include product attributes. Default = true.
        /// </summary>
        public bool IncludeProductAttributes { get; init; } = true;

        /// <summary>
        /// A value indicating whether to include gift card attributes. Default = true.
        /// </summary>
        public bool IncludeGiftCardAttributes { get; init; } = true;

        /// <summary>
        /// A value indicating whether to include HTML hyperlinks. Default = true.
        /// </summary>
        public bool IncludeHyperlinks { get; init; } = true;
    }
}
