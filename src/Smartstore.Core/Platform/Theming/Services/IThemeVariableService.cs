using System.Dynamic;

namespace Smartstore.Core.Theming
{
    public interface IThemeVariableService
    {
        /// <summary>
        /// Gets a dynamic object which holds all runtime theme variables
        /// </summary>
        /// <param name="themeName">The theme to get variables for.</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>The dynamic variables object</returns>
        Task<ExpandoObject> GetThemeVariablesAsync(string themeName, int storeId);

        /// <summary>
        /// Saves theme variables to the database
        /// </summary>
        /// <param name="themeName">The theme for which to save variables</param>
		/// <param name="storeId">Store identifier</param>
        /// <param name="variables">The variables to save</param>
        /// <returns>The count of successfully updated or inserted variables</returns>
		Task<int> SaveThemeVariablesAsync(string themeName, int storeId, IDictionary<string, object> variables);

        /// <summary>
        /// Deletes all variables of the specified theme from the database
        /// </summary>
		/// <param name="themeName">The theme to get variables for.</param>
		/// <param name="storeId">Store identifier</param>
		Task DeleteThemeVariablesAsync(string themeName, int storeId);

        /// <summary>
        /// Imports variables from xml
        /// </summary>
        /// <param name="themeName">The theme for which to import variables</param>
		/// <param name="storeId">Store identifier</param>
        /// <param name="configurationXml">The xml configuration</param>
        /// <returns>The number of successfully imported variables</returns>
		Task<int> ImportVariablesAsync(string themeName, int storeId, string configurationXml);

        /// <summary>
        /// Exports the configuration of a theme to xml
        /// </summary>
        /// <param name="themeName">The theme to export variables for</param>
		/// <param name="storeId">Store identifier</param>
        /// <returns>The configuration xml</returns>
		Task<string> ExportVariablesAsync(string themeName, int storeId);
    }
}
