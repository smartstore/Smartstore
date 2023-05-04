#nullable enable

namespace Smartstore.Core.Web
{
    public class UserAgentParserOptions 
    {
        public const string DefaultYamlPath = "App_Data/useragent.yml";

        /// <summary>
        /// Get or set location for YAML file (relative to application content root) 
        /// </summary>
        public string YamlFilePath { get; set; } = DefaultYamlPath;
    }
    
    /// <summary>
    /// Responsible for parsing and materializing a user agent string.
    /// </summary>
    public interface IUserAgentParser
    {
        UserAgentInfo Parse(string? userAgent);
    }
}
