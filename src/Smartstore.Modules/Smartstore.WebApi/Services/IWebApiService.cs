using Smartstore.WebApi.Models;

namespace Smartstore.WebApi.Services
{
    public partial interface IWebApiService
    {
        WebApiState GetState();

        // TODO: (mg) (core) I am sure this one is not necessary.
        Task<WebApiState> GetStateAsync();
    }
}
