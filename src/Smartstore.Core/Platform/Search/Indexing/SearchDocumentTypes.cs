namespace Smartstore.Core.Search.Indexing
{
    /// <summary>
    /// Known tokens of search index document types.
    /// </summary>
    public static partial class SearchDocumentTypes
    {
        public const string Product = "p";
        public const string Category = "c";
        public const string Manufacturer = "m";
        public const string DeliveryTime = "dt";
        public const string Attribute = "a";
        public const string AttributeValue = "av";
        public const string Variant = "v";
        public const string VariantValue = "vv";
        public const string Customer = "u";
        public const string Forum = "f";
        public const string ForumPost = "fp";
    }
}
