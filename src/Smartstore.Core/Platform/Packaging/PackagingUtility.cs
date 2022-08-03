using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Packaging
{
    internal static class PackagingUtility
    {
        public const string ManifestFileName = "manifest.json";

        public static string BuildExtensionPrefix(ExtensionType extensionType)
            => string.Format("Smartstore.{0}.", extensionType.ToString());

        public static string BuildPackageName(this IExtensionDescriptor descriptor)
            => BuildExtensionPrefix(descriptor.ExtensionType) + BuildExtensionId(descriptor);

        public static string BuildExtensionId(this IExtensionDescriptor descriptor)
            => descriptor.Name + '.' + descriptor.Version.ToString();

        public static string ExtensionRoot(this IExtensionDescriptor descriptor)
            => descriptor.ExtensionType == ExtensionType.Theme ? "Themes" : "Modules";

        public static bool IsTheme(this IExtensionDescriptor descriptor)
            => descriptor.ExtensionType == ExtensionType.Theme;

        public static bool IsTheme(string packageFileName)
            => packageFileName.StartsWith(BuildExtensionPrefix(ExtensionType.Theme));

        public static string GetExtensionPath(this IExtensionDescriptor descriptor)
        {
            string subpath;

            if (descriptor is IExtensionLocation location)
            {
                subpath = location.Path;
            }
            else
            {
                subpath = descriptor.ExtensionType == ExtensionType.Theme ? "/Themes/" : "/Modules/";
                subpath += descriptor.Name;
            }

            return subpath;
        }

        /// <summary>
        /// Gets a value indicating whether an extension is assumed
        /// to be compatible with the current app version
        /// </summary>
        /// <remarks>
        /// An extension is generally compatible when both app version and extension's 
        /// <c>MinorAppVersion</c> are equal, OR - when app version is greater - it is 
        /// assumed to be compatible when no breaking changes occured since <c>MinorAppVersion</c>.
        /// </remarks>
        /// <param name="descriptor">The descriptor of extension to check</param>
        /// <returns><c>true</c> when the extension is assumed to be compatible</returns>
        public static bool IsAssumedCompatible(IExtensionDescriptor descriptor)
            => SmartstoreVersion.IsAssumedCompatible(descriptor?.MinAppVersion);
    }
}
