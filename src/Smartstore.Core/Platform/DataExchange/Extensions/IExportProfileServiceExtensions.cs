using System.Threading.Tasks;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export
{
    public static partial class IExportProfileServiceExtensions
    {
        /// <summary>
        /// Gets the log file for an export profile.
        /// </summary>
        /// <param name="exportProfileService">Export profile service.</param>
        /// <param name="profile">Export profile.</param>
        /// <returns>Log file.</returns>
        public static async Task<IFile> GetLogFileAsync(this IExportProfileService exportProfileService, ExportProfile profile)
        {
            Guard.NotNull(exportProfileService, nameof(exportProfileService));

            var dir = await exportProfileService.GetExportDirectoryAsync(profile, null, false);
            var logFile = await dir.FileSystem.GetFileAsync(dir.FileSystem.PathCombine(dir.SubPath, "log.txt"));

            return logFile;
        }
    }
}
