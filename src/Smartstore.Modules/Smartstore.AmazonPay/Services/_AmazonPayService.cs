using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.AmazonPay.Services
{
    public partial class AmazonPayService : IAmazonPayService
    {
        public AmazonPayService()
        {
        }

        /// <summary>
        /// Also named "spId".
        /// </summary>
        internal static string PlatformId => "A3OJ83WFYM72IY";
        internal static string LeadCode => "SPEXDEAPA-SmartStore.Net-CP-DP";

        public Task RunDataPollingAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException();
        }

        #region Utilities

        internal static string GetAmazonLanguageCode(string twoLetterLanguageCode, char delimiter = '-')
        {
            return twoLetterLanguageCode.EmptyNull().ToLower() switch
            {
                "en" => $"en{delimiter}GB",
                "fr" => $"fr{delimiter}FR",
                "it" => $"it{delimiter}IT",
                "es" => $"es{delimiter}ES",
                _ => $"de{delimiter}DE",
            };
        }


        #endregion
    }
}
