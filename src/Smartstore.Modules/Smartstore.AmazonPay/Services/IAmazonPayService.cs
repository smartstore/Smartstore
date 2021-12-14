using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.AmazonPay.Services
{
    public interface IAmazonPayService
    {
        Task RunDataPollingAsync(CancellationToken cancelToken = default);
        Task<int> UpdateAccessKeysAsync(string json, int storeId);
    }
}
