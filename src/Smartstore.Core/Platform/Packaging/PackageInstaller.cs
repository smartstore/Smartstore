using System.IO.Compression;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Theming;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Core.Packaging
{
    public partial class PackageInstaller : IPackageInstaller
    {
        private readonly IApplicationContext _appContext;
        private readonly IThemeRegistry _themeRegistry;
        private readonly INotifier _notifier;

        public PackageInstaller(IApplicationContext appContext, IThemeRegistry themeRegistry, INotifier notifier)
        {
            _appContext = appContext;
            _themeRegistry = themeRegistry;
            _notifier = notifier;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task<IExtensionDescriptor> InstallPackageAsync(ExtensionPackage package)
        {
            // TODO: (core) Check if required dependencies are already installed.

            ZipArchive archive = null;
            try
            {
                archive = new ZipArchive(package.ArchiveStream, ZipArchiveMode.Read);
            }
            catch (Exception ex)
            {
                archive?.Dispose();
                Logger.Error(T("Admin.Packaging.StreamError"), ex);
                throw new IOException(T("Admin.Packaging.StreamError"), ex);
            }

            using (archive)
            {
                return await InstallPackageCore(package, archive);
            }
        }

        protected async Task<IExtensionDescriptor> InstallPackageCore(ExtensionPackage package, ZipArchive archive)
        {
            // *** Check if extension is compatible with current app version
            if (!SmartstoreVersion.IsAssumedCompatible(package.Descriptor.MinAppVersion))
            {
                var msg = T("Admin.Packaging.IsIncompatible", SmartstoreVersion.CurrentFullVersion);
                Logger.Error(msg);
                throw new InvalidOperationException(msg);
            }

            IExtensionDescriptor installedExtension;
            IDirectory backupDirectory;

            // *** See if extension was previously installed and backup its directory if so
            try
            {
                TryBackupExtension(package.Descriptor, out installedExtension, out backupDirectory);
            }
            catch (Exception ex)
            {
                Logger.Error(T("Admin.Packaging.BackupError"), ex);
                throw new IOException(T("Admin.Packaging.BackupError"), ex);
            }

            if (installedExtension != null && package.Descriptor.Version != installedExtension.Version)
            {
                // *** If extension is installed and version differs, need to uninstall first. In case of matching version we gonna merge files.
                try
                {
                    await UninstallExtensionAsync(installedExtension);
                }
                catch (Exception ex)
                {
                    Logger.Error(T("Admin.Packaging.UninstallError"), ex);
                    throw new Exception(T("Admin.Packaging.UninstallError"), ex);
                }
            }

            // *** Extract archive to destination
            var isTheme = package.Descriptor.IsTheme();
            try
            {
                if (isTheme)
                {
                    // Avoid getting terrorized by IO events.
                    _themeRegistry.StopMonitoring();
                }

                await ExtractArchive(archive);

                if (isTheme)
                {
                    _themeRegistry.ReloadThemes();
                }
            }
            catch (Exception ex)
            {
                if (installedExtension != null)
                {
                    // Restore the previous version
                    RestoreExtension(installedExtension, backupDirectory);
                }
                else
                {
                    // Just remove the new package
                    await UninstallExtensionAsync(package.Descriptor);
                }

                Logger.Error(ex);
                throw;
            }
            finally
            {
                if (isTheme)
                {
                    // SOFT start IO events again.
                    _themeRegistry.StartMonitoring(false);
                }
            }

            if (installedExtension == null && isTheme)
            {
                installedExtension = _themeRegistry.GetThemeDescriptor(package.Descriptor.Name);
            }

            return installedExtension ?? package.Descriptor;
        }

        public async Task UninstallExtensionAsync(IExtensionDescriptor extension)
        {
            var path = extension.GetExtensionPath();
            var dir = await _appContext.ContentRoot.GetDirectoryAsync(path);

            if (!dir.Exists)
            {
                throw new IOException(T("Admin.Packaging.NotFound", path));
            }

            // TODO: (core) The descriptor should also be removed from ThemeRegistry or ModuleCatalog (but how?)
            await dir.DeleteAsync();
        }

        private async Task ExtractArchive(ZipArchive archive)
        {
            var fs = _appContext.ContentRoot;

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EqualsNoCase(PackagingUtility.ManifestFileName))
                    continue;

                if (Path.GetFileName(entry.FullName).Length == 0)
                {
                    // Entry is a directory
                    if (entry.Length == 0)
                    {
                        fs.TryCreateDirectory(entry.FullName);
                    }
                }
                else
                {
                    // Entry is a file
                    using var entryStream = entry.Open();
                    await fs.CreateFileAsync(entry.FullName, entryStream, true);
                }
            }
        }

        #region Backup/Restore

        private bool TryBackupExtension(IExtensionDescriptor extension, out IExtensionDescriptor installedExtension, out IDirectory backupDirectory)
        {
            installedExtension = null;
            backupDirectory = null;

            var fs = _appContext.ContentRoot;
            var path = extension.GetExtensionPath().Trim('/', '\\');
            var sourceDirectory = _appContext.ContentRoot.GetDirectory(path);

            if (sourceDirectory.Exists)
            {
                installedExtension = extension.ExtensionType == ExtensionType.Theme
                    ? _themeRegistry.GetThemeDescriptor(extension.Name)
                    : _appContext.ModuleCatalog.GetModuleByName(extension.Name, false);

                var backupRoot = "App_Data/_Backup";
                var uniqueDirName = fs.CreateUniqueDirectoryName(backupRoot, path, 100);

                if (uniqueDirName == null)
                {
                    throw new InvalidOperationException(T("Admin.Packaging.TooManyBackups", PathUtility.Join(backupRoot, path)));
                }

                uniqueDirName = PathUtility.Join(backupRoot, uniqueDirName);
                fs.TryCreateDirectory(uniqueDirName);

                backupDirectory = fs.GetDirectory(uniqueDirName);
                fs.CopyDirectory(sourceDirectory, backupDirectory, true);

                _notifier.Information(T("Admin.Packaging.BackupSuccess", backupDirectory.Name));

                return true;
            }

            return false;
        }

        private bool RestoreExtension(IExtensionDescriptor extension, IDirectory backupDirectory)
        {
            if (backupDirectory == null || !backupDirectory.Exists)
            {
                return false;
            }

            var fs = _appContext.ContentRoot;
            var destinationDirectory = fs.GetDirectory(extension.GetExtensionPath());
            if (!destinationDirectory.Exists)
            {
                fs.TryCreateDirectory(destinationDirectory.SubPath);
            }

            fs.CopyDirectory(backupDirectory, destinationDirectory, true);

            _notifier.Information(T("Admin.Packaging.RestoreSuccess", destinationDirectory.SubPath));

            return true;
        }

        #endregion
    }
}
