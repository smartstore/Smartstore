using Amazon.Pay.API.WebStore.ChargePermission;
using Amazon.Pay.API.WebStore.Interfaces;
using Microsoft.Extensions.Logging;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders.Events;
using Smartstore.Engine.Modularity;
using Smartstore.Events;

namespace Smartstore.AmazonPay
{
    public class Events : IConsumer
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public Task HandleEventAsync(OrderPaidEvent message,
            ICommonServices services,
            IProviderManager providerManager,
            ILogger logger,
            Lazy<IWebStoreClient> apiClient)    // TODO: (mg) (core) that's wrong. Use message.Order.StoreId.
        {
            var order = message.Order;

            if (order != null
                && order.PaymentMethodSystemName.EqualsNoCase(AmazonPayProvider.SystemName)
                && order.AuthorizationTransactionCode.HasValue())
            {
                var module = services.ApplicationContext.ModuleCatalog.GetModuleByAssembly(typeof(Events).Assembly);

                if (providerManager.IsActiveForStore(module, order.StoreId))
                {
                    try
                    {
                        var request = new CloseChargePermissionRequest(T("Plugins.Payments.AmazonPay.CloseChargeReason").Value.Truncate(255))
                        {
                            CancelPendingCharges = false
                        };

                        var response = apiClient.Value.CloseChargePermission(order.AuthorizationTransactionCode, request);
                        if (!response.Success)
                        {
                            logger.LogAmazonPayFailure(request, response);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
