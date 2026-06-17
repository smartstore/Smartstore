using Smartstore.Core.Configuration;
using Smartstore.Utilities;

namespace Smartstore.Core.Security;

public class SecuritySettings : ISettings
{
    /// <summary>
    /// When <c>true</c>, bypasses any SSL redirection on localhost
    /// </summary>
    [GlobalSetting]
    public bool UseSslOnLocalhost { get; set; }

    /// <summary>
    /// Gets or sets an encryption key
    /// </summary>
    [GlobalSetting]
    public string EncryptionKey { get; set; } = CommonHelper.GenerateRandomDigitCode(16);

    /// <summary>
    /// Gets or sets a list of adminn area allowed IP addresses
    /// </summary>
    [GlobalSetting]
    public List<string> AdminAreaAllowedIpAddresses { get; set; } = [];

    /// <summary>
    /// Gets or sets a vaule indicating whether to hide admin menu items based on ACL
    /// </summary>
    [GlobalSetting]
    public bool HideAdminMenuItemsBasedOnPermissions { get; set; }

    /// <summary>
    /// Gets or sets a vaule indicating whether "Honeypot" is enabled to prevent bots from posting forms.
    /// </summary>
    [GlobalSetting]
    public bool EnableHoneypotProtection { get; set; }
}