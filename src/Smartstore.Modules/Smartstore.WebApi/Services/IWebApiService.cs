using Smartstore.WebApi.Models;

namespace Smartstore.WebApi.Services
{
    public partial interface IWebApiService
    {
        WebApiState GetState(int? storeId = null);
        Task<Dictionary<string, WebApiUser>> GetApiUsersAsync();
    }
}
