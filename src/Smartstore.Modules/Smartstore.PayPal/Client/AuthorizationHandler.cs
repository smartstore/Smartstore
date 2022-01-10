using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Configuration;

namespace Smartstore.PayPal.Client
{
    internal class AuthorizationHandler : DelegatingHandler
    {
        private readonly ISettingFactory _settingFactory;
        
        public AuthorizationHandler(ISettingFactory settingFactory)
        {
            // INFO: (mh) (core) We could have injected PayPalSettings here directly, but AuthorizationHandler
            // instance will live for 2 minutes as part of the primary message handler. SO: getting fresh settings
            // on every call is the better option.
            _settingFactory = settingFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains("Authorization"))
            {
                // TODO: (mh) (core) Implement Authorization here as it is done in Checkout-NET-SDK.
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "TEST123456789");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
