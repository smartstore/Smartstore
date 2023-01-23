using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Orders.Events;
using Smartstore.Core.Localization;
using Smartstore.Core;
using Smartstore.Engine.Modularity;
using Smartstore.Events;
using SmartStore.DellyManLogistics.Client;
using Smartstore.Shipping.Settings;
using Autofac.Core;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using SmartStore.DellyManLogistics.Models;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Refit;

namespace SmartStore.DellyManLogistics.Events
{
    public class EvenConsumer : IConsumer
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task HandleEventAsync(OrderPlacedEvent message,
            IDellyManClient client,
            ICommonServices services,
            DellyManLogisticsSettings dellyManLogisticsSettings,
            IProviderManager providerManager,
            CancellationToken cancelToken)
        {
            var module = services.ApplicationContext.ModuleCatalog.GetModuleByAssembly(typeof(EvenConsumer).Assembly);


            //message.Order.AddOrderNote(T("Plugins.Sms.Clickatell.SmsSentNote"));
            //await services.DbContext.SaveChangesAsync(cancelToken);
        }

        public async Task HandleEventAsync(OrderPaidEvent eventMessage,
            IDellyManClient client,
            ICommonServices services,
            DellyManLogisticsSettings dellyManLogisticsSettings,
            IProviderManager providerManager,
            ILogger<EvenConsumer> logger,
            CancellationToken cancelToken)
        {
            var module = services.ApplicationContext.ModuleCatalog.GetModuleByAssembly(typeof(EvenConsumer).Assembly);
            if (eventMessage?.Order == null)
                return;


            try
            {
                var settings = dellyManLogisticsSettings;
                string customerUsername = eventMessage.Order.Customer.Username;
                var orderDate = DateTime.Now.ToString("yyyy/MM/dd");
                var bookOrder = new DellyManBookOrderModel
                {
                    CompanyID = int.Parse(settings.CompanyId),
                    CustomerID = int.Parse(settings.CustomerId),
                    OrderRef = "",
                    DeliveryRequestedDate = orderDate,
                    DeliveryRequestedTime = orderDate,
                    IsInstantDelivery = 0,
                    Packages = eventMessage.Order.OrderItems.Select(item => new DellyManPackageModel
                    {
                        DeliveryCity = eventMessage.Order.Customer.ShippingAddress.City,
                        DeliveryContactName =$"{eventMessage.Order.Customer.ShippingAddress.FirstName} {eventMessage.Order.Customer.ShippingAddress.LastName}",
                        DeliveryContactNumber = eventMessage.Order.ShippingAddress.PhoneNumber,
                        DeliveryGooglePlaceAddress = eventMessage.Order.Customer.ShippingAddress.Address1,
                        DeliveryLandmark = "",
                        DeliveryState = eventMessage.Order.Customer.ShippingAddress.StateProvince.Name,
                        PackageDescription = item.Product.Name,
                        PickUpCity = settings.DefaultPickUpCity,
                        PickUpState = settings.DefaultPickUpState

                    }).ToList(),
                    PaymentMode = "pickup",
                    PickUpContactName = settings.DefaultPickUpContactName,
                    PickUpContactNumber = settings.DefaultPickUpContactNumber,
                    PickUpGooglePlaceAddress = settings.DefaultPickUpGoogleAddress,
                    PickUpLandmark = "",
                    PickUpRequestedDate = orderDate,
                    PickUpRequestedTime = settings.PickupRequestedTime,
                    Vehicle = "Bike"
                };
                var jsonRequest = bookOrder.ToString();
                logger.LogInformation("DellyMan Request for Customer {0} is {1}", customerUsername, jsonRequest);

                var request = await client.BookOrderAsync(bookOrder);


                if (!request.IsSuccessStatusCode)
                {
                    var errorContent = request.Error.Content;
                    logger.LogError(errorContent);
                    logger.LogError(request.Error,"");
                    return;
                }

                if(request.Content!= null)
                {
                    logger.LogError(request.Error.InnerException, "");
                    return;
                }

                var response = request.Content;
                if (response.ResponseCode == 100 && response.ResponseMessage.Equals("Success", StringComparison.InvariantCultureIgnoreCase))
                {
                    services.DbContext.Shipments.Add(new Shipment
                    {
                        TrackingNumber = response.TrackingID.ToString(),
                        TrackingUrl = string.Format("{0}?id={1}", settings.OrderTrackingUrl, response.TrackingID),
                        OrderId = eventMessage.Order.Id,
                        CreatedOnUtc = DateTime.Now
                    });

                }


                eventMessage.Order.OrderNotes.Add(new Smartstore.Core.Checkout.Orders.OrderNote
                {
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.Now,
                    Note = $"Order Shipping with Reference:{response.Reference}, OrderId:{response.OrderID}, TrackingId:{response.TrackingID}"
                });
                await services.DbContext.SaveChangesAsync(cancelToken);


            }
            catch(ApiException refitException)
            {
                var error = refitException.Content;
                logger.LogError(error, "");
                logger.LogError(refitException, "");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "");
            }


        }
    }
}
