using System.Threading.Tasks;

namespace Smartstore.Core.DataExchange.Import
{
    public partial interface IImportProfileService
    {
        /// <summary>
        /// Inserts an import profile.
        /// </summary>
        /// <param name="fileName">Name of the import file</param>
        /// <param name="name">Profile name</param>
        /// <param name="entityType">Entity type</param>
        /// <returns>Inserted import profile</returns>
        Task<ImportProfile> InsertImportProfileAsync(string fileName, string name, ImportEntityType entityType);

        /// <summary>
        /// Gets a new profile name.
        /// </summary>
        /// <param name="entityType">Entity type</param>
        /// <returns>Suggestion for a new profile name.</returns>
        Task<string> GetNewProfileNameAsync(ImportEntityType entityType);
    }
}
