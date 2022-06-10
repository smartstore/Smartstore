using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export
{
    public partial interface IExportProfileService
    {
        /// <summary>
        /// Gets a temporary directory for an export profile.
        /// </summary>
        /// <param name="profile">Export profile.</param>
        /// <param name="subpath">Optional subpath, e.g. "Content" to get the content subfolder.</param>
        /// <param name="createIfNotExists">A value indicating whether the folder should be created if it does not exist.</param>
        /// <returns>Export directory.</returns>
        Task<IDirectory> GetExportDirectoryAsync(ExportProfile profile, string subpath = null, bool createIfNotExists = false);

        /// <summary>
        /// Gets the directory for deploying export files. <c>null</c> if the profile has no deployment based on file system.
        /// </summary>
        /// <param name="deployment">Export deployment.</param>
        /// <param name="createIfNotExists">A value indicating whether the folder should be created if it does not exist.</param>
        /// <returns>Deploement directory.</returns>
        Task<IDirectory> GetDeploymentDirectoryAsync(ExportDeployment deployment, bool createIfNotExists = false);

        /// <summary>
        /// Gets the URL of the public export directory. <c>null</c> if the profile has no public deployment.
        /// </summary>
        /// <param name="deployment">Export deployment.</param>
        /// <param name="store">Store to get the domain from. If <c>null</c>, store will be obtained from <see cref="ExportProfile.Filtering"/> or <see cref="ExportProfile.Projection"/>,
        /// or from <see cref="IStoreContext.CurrentStore"/>, if no store information was found at all.</param>
        /// <returns>URL of the public export directory.</returns>
        Task<string> GetDeploymentDirectoryUrlAsync(ExportDeployment deployment, Store store = null);

        /// <summary>
        /// Adds an export profile.
        /// </summary>
        /// <param name="provider">Export provider.</param>
        /// <param name="isSystemProfile">A value indicating whether the new profile is a system profile.</param>
        /// <param name="profileSystemName">Profile system name.</param>
        /// <param name="cloneFromProfileId">Identifier of a profile the settings should be copied from.</param>
        /// <returns>Added export profile.</returns>
        Task<ExportProfile> InsertExportProfileAsync(
            Provider<IExportProvider> provider,
            bool isSystemProfile = false,
            string profileSystemName = null,
            int cloneFromProfileId = 0);

        /// <summary>
        /// Adds an export profile.
        /// </summary>
        /// <param name="providerSystemName">Provider system name. Must not be empty.</param>
        /// <param name="name">The name of the profile.</param>
        /// <param name="fileExtension">The file extension supported by the export provider.</param>
        /// <param name="features">Features supported by the export provider.</param>
        /// <param name="isSystemProfile">A value indicating whether the new profile is a system profile.</param>
        /// <param name="profileSystemName">Profile system name.</param>
        /// <param name="cloneFromProfileId">Identifier of a profile the settings should be copied from.</param>
        /// <returns>Added export profile.</returns>
        Task<ExportProfile> InsertExportProfileAsync(
            string providerSystemName,
            string name,
            string fileExtension,
            ExportFeatures features,
            bool isSystemProfile = false,
            string profileSystemName = null,
            int cloneFromProfileId = 0);

        /// <summary>
        /// Deletes an export profile.
        /// </summary>
        /// <param name="profile">Export profile.</param>
        /// <param name="force">A value indicating whether to delete system profiles.</param>
        Task DeleteExportProfileAsync(ExportProfile profile, bool force = false);

        /// <summary>
        /// Loads all export providers.
        /// </summary>
        /// <param name="storeId">Filter providers that are only active for the store specified by this identifier.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden providers.</param>
        /// <returns>Export providers.</returns>
        IEnumerable<Provider<IExportProvider>> LoadAllExportProviders(int storeId = 0, bool includeHidden = true);

        /// <summary>
        /// Deletes the export files of all export profiles.
        /// </summary>
        /// <param name="startDate">Delete only files whose creation date is greater than the specified date.</param>
        /// <param name="endDate">Delete only files whose creation date is less than the specified date</param>
        /// <returns>Number of deleted files and folders.</returns>
        Task<(int DeletedFiles, int DeletedFolders)> DeleteExportFilesAsync(DateTime? startDate, DateTime? endDate);
    }
}
