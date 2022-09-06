using Smartstore.WebApi.Models;

namespace Smartstore.WebApi.Services
{
    public partial interface IWebApiService
    {
        WebApiState GetState();

        Task<WebApiState> GetStateAsync();
    }
}
