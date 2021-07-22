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
        
        public static string GetExtensionPrefix(ExtensionType extensionType)
            => string.Format("Smartstore.{0}.", extensionType.ToString());

        public static string BuildPackageFileName(IExtensionDescriptor descriptor)
            => GetExtensionPrefix(descriptor.ExtensionType) + descriptor.Name + '.' + descriptor.Version.ToString() + ".zip";

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
