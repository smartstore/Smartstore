using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        //public static bool IsTheme(this PackageInfo info)
        //    => IsTheme(info.Id);

        //public static bool IsTheme(string packageId)
        //    => packageId.StartsWith(GetExtensionPrefix("Theme"));

        //internal static string ExtensionFolder(this PackageInfo package)
        //    => ExtensionDirectoryName(package.IsTheme());

        //internal static string ExtensionId(this PackageInfo package)
        //    => ExtensionId(package.IsTheme(), package.Id);

        //private static string ExtensionDirectoryName(bool isTheme)
        //    => isTheme ? "Themes" : "Modules";

        //private static string ExtensionId(bool isTheme, string packageId)
        //{
        //    return isTheme
        //        ? packageId[GetExtensionPrefix("Theme").Length..]
        //        : packageId[GetExtensionPrefix("Module").Length..];
        //}

        //// TODO: (core) Unfortunately "ThemeManifest" is in an unreferenced assembly. Refactor!
        //internal static ExtensionDescriptor ConvertToExtensionDescriptor(this ThemeManifest themeManifest)
        //{
        //    var descriptor = new ExtensionDescriptor
        //    {
        //        ExtensionType = "Theme",
        //        Location = "~/Themes",
        //        Path = themeManifest.Path,
        //        Id = themeManifest.ThemeName,
        //        Author = themeManifest.Author.HasValue() ? themeManifest.Author : "[Unknown]",
        //        MinAppVersion = SmartstoreVersion.Version, // TODO: (pkg) Add SupportedVersion to theme manifests
        //        Version = new Version(themeManifest.Version),
        //        Name = themeManifest.ThemeTitle,
        //        Description = string.Empty, // TODO: (pkg) Add description to theme manifests
        //        WebSite = themeManifest.Url,
        //        Tags = string.Empty // TODO: (pkg) Add tags to theme manifests,
        //    };

        //    return descriptor;
        //}

        //// TODO: (core) Implement GetExtensionDescriptor().
        //internal static ExtensionDescriptor GetExtensionDescriptor(this IPackageMetadata package, string extensionType)
        //{
        //    bool isTheme = extensionType.EqualsNoCase("Theme");

        //    IPackageFile packageFile = package.GetFiles().FirstOrDefault(file =>
        //    {
        //        var fileName = Path.GetFileName(file.Path);
        //        return fileName != null && fileName.Equals(isTheme ? "theme.config" : "Description.txt", StringComparison.OrdinalIgnoreCase);
        //    });

        //    ExtensionDescriptor descriptor = null;

        //    if (packageFile != null)
        //    {
        //        var filePath = packageFile.EffectivePath;
        //        if (filePath.HasValue())
        //        {
        //            filePath = Path.Combine(HostingEnvironment.MapPath("~/"), filePath);
        //            if (isTheme)
        //            {
        //                var themeManifest = ThemeManifest.Create(Path.GetDirectoryName(filePath));
        //                if (themeManifest != null)
        //                {
        //                    descriptor = themeManifest.ConvertToExtensionDescriptor();
        //                }
        //            }
        //            else // is a Plugin
        //            {
        //                var pluginDescriptor = PluginFileParser.ParsePluginDescriptionFile(filePath);
        //                if (pluginDescriptor != null)
        //                {
        //                    descriptor = pluginDescriptor.ConvertToExtensionDescriptor();
        //                }
        //            }
        //        }
        //    }

        //    return descriptor;
        //}
    }
}
