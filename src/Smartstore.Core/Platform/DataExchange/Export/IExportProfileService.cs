using System.Threading.Tasks;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.DataExchange.Export
{
    public partial interface IExportProfileService
    {
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
    }
}
