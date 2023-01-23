using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaxMind.GeoIP2.Model;
using Refit;
using SmartStore.DellyManLogistics.Models;

namespace SmartStore.DellyManLogistics.Client
{
    public interface IDellyManClient
    {

        [Post("/GetQuotes")]
        [Headers("Content-Type: application/json")]
        Task<ApiResponse<DellyManBaseResponseModel<List<DellyManCompanyModel>>>> GetQuotesAsync([Body] GetQuoteRequestModel model);


        [Post("/BookOrder")]
        [Headers("Content-Type: application/json")]
        Task<ApiResponse<DellyManBookOrderResponseModel>> BookOrderAsync([Body] DellyManBookOrderModel model);


        [Get("/States")]
        [Headers("Content-Type: application/json")]
        Task<ApiResponse<List<Smartstore.Web.Models.DellyMan.State>>> GetStatesAsync();

        //[Post("/Cities?StateID={stateId}")]
        //Task<ApiResponse<List<Smartstore.Web.Models.DellyMan.City>>> GetCitiesAsync(string stateId);

    }
}
