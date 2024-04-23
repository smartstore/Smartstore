#nullable enable

namespace Smartstore.Web.Api
{
    /// <summary>
    /// Represents OpenAPI document names for the Web API. Applicable for <see cref="WebApiGroupAttribute(string, string)" />.
    /// </summary>
    /// <remarks>Names must be globally unique, URI-friendly and should be in lower case.</remarks>
    public static class WebApiGroupNames
    {
        public const string Platform = "platform";
        public const string Common = "common";
        public const string Identity = "identity";
        public const string Catalog = "catalog";
        public const string Content = "content";
        public const string Checkout = "checkout";
        
        public static readonly string[] All =
        [
            Platform,
            Common,
            Identity,
            Catalog,
            Content,
            Checkout
        ];
    }

    /// <summary>
    /// Assigns endpoints to an API group for Swagger documentation.
    /// </summary>
    public class WebApiGroupAttribute : ApiExplorerSettingsAttribute
    {
        /// <param name="name">
        /// Must be one of <see cref="WebApiGroupNames"/> constants, otherwise it would not appear in Swagger documentation.
        /// </param>
        /// <param name="version">Version number. At the moment always 1.</param>
        public WebApiGroupAttribute(string name, string version = "1")
        {
            GroupName = name + version;
        }
    }
}
