using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api.Services
{
    public partial interface IWebApiService
    {
        WebApiState GetState(int? storeId = null);
        Task<Dictionary<string, WebApiUser>> GetApiUsersAsync();
    }
}
