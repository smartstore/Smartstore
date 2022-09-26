using Smartstore.Core.Stores;
using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api
{
    public partial interface IWebApiService
    {
        /// <summary>
        /// Gets the current state of the API including configuration settings.
        /// </summary>
        /// <param name="storeId">
        /// Store identifier to get the state for.
        /// If <c>null</c>, identifier will be obtained via <see cref="IStoreContext.CurrentStore"/>.
        /// </param>
        /// <returns>Web API state.</returns>
        WebApiState GetState(int? storeId = null);

        /// <summary>
        /// Gets a map with all users who have access to the API.
        /// The key is the public API access key of the user.
        /// </summary>
        /// <returns>Map of public access key to API user.</returns>
        Task<Dictionary<string, WebApiUser>> GetApiUsersAsync();

        void ClearApiUserCache();
    }
}
