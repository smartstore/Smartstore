using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Refit;
using Smartstore.Paystack.Models;

namespace Smartstore.Paystack.Client
{
    public interface IPaystackClient
    {
        [Post("/transaction/initialize")]
        Task<ApiResponse<PaystackResponseModel<PaystackInitializeResponseModel>>> InitializeAsync([Body] PaystackInitializeRequestModel model);

        [Get("/transaction/verify/{reference}")]
        Task <ApiResponse<PaystackResponseModel<VerifyTransactionResponseModel>>> VerifyAsync(string reference);
    
    }
}
