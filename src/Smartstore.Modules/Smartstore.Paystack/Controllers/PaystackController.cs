using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core;
using Smartstore.Paystack.Client;
using Smartstore.Paystack.Configuration;
using Smartstore.Paystack.Models;
using Smartstore.Web.Controllers;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Stores;
using Autofac.Core;
using Smartstore.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Paystack.Controllers
{

    public class PaystackController : PublicController
    {
        private readonly PaystackSettings _paystackSettings;
        private readonly SmartDbContext _db;
        private readonly IPaystackClient _paystackClient;
        private readonly IOrderProcessingService _orderProcessingService;

        public PaystackController(PaystackSettings paystackSettings,
             SmartDbContext db,
            IPaystackClient paystackClient,
            IOrderProcessingService orderProcessingService)
        {
            _db = db;
            _paystackSettings = paystackSettings;
            _paystackClient = paystackClient;
            _orderProcessingService = orderProcessingService;
        }


        public async Task<IActionResult> PaymentConfirmation(string trxref, string reference)
        {

            //var settings = _services.Settings.LoadSetting<PaystackSettings>(_services.StoreContext.CurrentStore.Id);
            var verifyResponse = await _paystackClient.VerifyAsync(reference);

            await verifyResponse.EnsureSuccessStatusCodeAsync();

            var order = await _db.Orders.FirstOrDefaultAsync(x => x.OrderGuid == Guid.Parse(reference));

            if (order == null)
            {
                return NotFound();
            }

            var paymentStatus = verifyResponse.Content.Data.Status;
            if (verifyResponse.Content.Status && verifyResponse.Content.Data.Status.Equals("success", StringComparison.InvariantCultureIgnoreCase))
            {

                if (order.AuthorizationTransactionId.IsEmpty())
                {
                    order.AuthorizationTransactionId = order.CaptureTransactionId = reference;
                    order.AuthorizationTransactionResult = order.CaptureTransactionResult = paymentStatus;

                }

                if (order.CanMarkOrderAsPaid())
                {
                    await _orderProcessingService.MarkOrderAsPaidAsync(order);
                }
                // Update order.
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(CheckoutController.Completed), "Checkout");
            }
            else
            {
                order.CaptureTransactionResult = paymentStatus;
                order.OrderStatus = OrderStatus.Pending;

                // Update order.
                await _db.SaveChangesAsync();
                return RedirectToAction("FailedPayment", "Paystack", new { area = "SmartStore.PaystackPayment", orderGuid = reference, status = verifyResponse.Content.Data.GatewayResponse });
            }

        }

        [HttpPost]
        public IActionResult WebHookCallBack(PaystackWebhook model)
        {
            try
            {
                var webhookString = model.ToString();
                //logger.Log(LogLevel.Information, null, webhookString, null);

                return Ok();
            }
            catch (Exception ex)
            {
                // Logger.Log(LogLevel.Error, ex, null, null);
                return StatusCode(500);
            }

        }

        public IActionResult FailedPayment(string orderGuid, string status)
        {
            return View();
            //validation
            //if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
            //    return new HttpUnauthorizedResult();

            ////model
            //var model = new PaymentFailedModel();


            //var order = GetOrder(orderGuid);
            ////var order = orderService.SearchOrders(_storeContext.CurrentStore.Id, _workContext.CurrentCustomer.Id,
            ////     null, null, null, null, null, null,orderGuid,null, 0, 1).FirstOrDefault();

            //if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
            //{
            //    return NotFound();
            //}

            //if (order.AuthorizationTransactionId.IsEmpty())
            //{
            //    order.AuthorizationTransactionId = order.CaptureTransactionId = orderGuid;
            //    order.AuthorizationTransactionResult = order.CaptureTransactionResult = status;

            //    _orderService.UpdateOrder(order);
            //}

            //model.OrderId = order.Id;
            //model.OrderNumber = order.GetOrderNumber();

            //orderService.AddOrderNote(order, status, true);

            ////  return RedirectToAction("Index", "Home", new { area = "" });

            //return View(model);

        }

    }
}
