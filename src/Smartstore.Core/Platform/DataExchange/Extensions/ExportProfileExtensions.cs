using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Export
{
    public static partial class ExportProfileExtensions
    {
        /// <summary>
        /// Gets a temporary folder for an export profile.
        /// </summary>
        /// <param name="profile">Export profile.</param>
        /// <returns>Folder path.</returns>
        public static string GetExportDirectory(this ExportProfile profile, bool content = false, bool create = false)
        {
            // TODO: (mg) (core) All file system related stuff must use IFileSystem. This method should return IDirectory. CommonHelper.ContentRoot could be used as entry point (or IApplicationContext.xyzRoot)
            // TODO: (mg) (core) Never save something outside of wwwroot or App_Data directories.
            // TODO: (mg) (core) Given the new requirements, this class has to be refactored rather heavily.
            Guard.NotNull(profile, nameof(profile));
            Guard.IsTrue(profile.FolderName.EmptyNull().Length > 2, nameof(profile.FolderName), "The export folder name must be at least 3 characters long.");

            var path = CommonHelper.MapPath(string.Concat(profile.FolderName, content ? "/Content" : ""));
            
            if (create && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// Gets the log file path for an export profile.
        /// </summary>
        /// <param name="profile">Export profile.</param>
        /// <returns>Log file path.</returns>
        public static string GetExportLogPath(this ExportProfile profile)
        {
            return Path.Combine(profile.GetExportDirectory(), "log.txt");
        }

        /// <summary>
        /// Gets the ZIP path for an export profile.
        /// </summary>
        /// <param name="profile">Export profile.</param>
        /// <returns>ZIP file path.</returns>
        public static string GetExportZipPath(this ExportProfile profile)
        {
            Guard.NotNull(profile, nameof(profile));

            var name = new DirectoryInfo(profile.FolderName).Name.NullEmpty() ?? "ExportData";

            return Path.Combine(profile.GetExportDirectory(), name.ToValidFileName() + ".zip");
        }

        /// <summary>
        /// Gets existing export files for an export profile.
        /// </summary>
        /// <param name="profile">Export profile.</param>
        /// <param name="provider">Export provider.</param>
        /// <returns>List of file names.</returns>
        public static IEnumerable<string> GetExportFiles(this ExportProfile profile, Provider<IExportProvider> provider)
        {
            var exportFolder = profile.GetExportDirectory(true);

            if (Directory.Exists(exportFolder) && provider.Value.FileExtension.HasValue())
            {
                var filter = "*.{0}".FormatInvariant(provider.Value.FileExtension.ToLower());

                return Directory.EnumerateFiles(exportFolder, filter, SearchOption.AllDirectories).OrderBy(x => x);
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Resolves the file name pattern for an export profile.
        /// </summary>
        /// <param name="profile">Export profile.</param>
        /// <param name="store">Store.</param>
        /// <param name="fileIndex">One based file index.</param>
        /// <param name="maxFileNameLength">The maximum length of the file name.</param>
        /// <returns>Resolved file name pattern.</returns>
        public static string ResolveFileNamePattern(this ExportProfile profile, Store store, int fileIndex, int maxFileNameLength)
        {
            using var psb = StringBuilderPool.Instance.Get(out var sb);
            sb.Append(profile.FileNamePattern);

            sb.Replace("%Profile.Id%", profile.Id.ToString());
            sb.Replace("%Profile.FolderName%", profile.FolderName);
            sb.Replace("%Store.Id%", store.Id.ToString());
            sb.Replace("%File.Index%", fileIndex.ToString("D4"));

            if (profile.FileNamePattern.Contains("%Profile.SeoName%"))
            {
                sb.Replace("%Profile.SeoName%", SeoHelper.BuildSlug(profile.Name, true, false, false).Replace("-", ""));
            }
            if (profile.FileNamePattern.Contains("%Store.SeoName%"))
            {
                sb.Replace("%Store.SeoName%", profile.PerStore ? SeoHelper.BuildSlug(store.Name, true, false, true) : "allstores");
            }
            if (profile.FileNamePattern.Contains("%Random.Number%"))
            {
                sb.Replace("%Random.Number%", CommonHelper.GenerateRandomInteger().ToString());
            }
            if (profile.FileNamePattern.Contains("%Timestamp%"))
            {
                sb.Replace("%Timestamp%", DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture));
            }

            var result = sb.ToString()
                .ToValidFileName(string.Empty)
                .Truncate(maxFileNameLength);

            return result;
        }
    }
}
