using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Refit;

namespace Smartstore.Core.RefitClients.DellyMan
{
    public interface IDellyManRefitClient
    {
        [Post("/Cities")]
        [Headers("Content-Type: application/json")]
        Task<ApiResponse<List<City>>> GetCitiesAsync([Body] GetCityRequestModel model);

    }
}
