using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.OfflinePayment.Controllers
{
    public class OfflinePaymentController : AdminController
    {
        private readonly IProviderManager _providerManager;

        public OfflinePaymentController(IProviderManager providerManager)
        {
            _providerManager = providerManager;
        }

        #region CashOnDelivery

        [LoadSetting, AuthorizeAdmin]
        public IActionResult CashOnDeliveryConfigure(CashOnDeliveryPaymentSettings settings)
        {
            var model = MiniMapper.Map<CashOnDeliveryPaymentSettings, CashOnDeliveryConfigurationModel>(settings);
            model.PostActionName = nameof(CashOnDeliveryConfigure);

            PrepareViewBag("Payments.CashOnDelivery");

            return View("GenericConfigure", model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult CashOnDeliveryConfigure(CashOnDeliveryConfigurationModel model, CashOnDeliveryPaymentSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return CashOnDeliveryConfigure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(CashOnDeliveryConfigure));
        }

        #endregion

        #region Invoice

        [LoadSetting, AuthorizeAdmin]
        public IActionResult InvoiceConfigure(InvoicePaymentSettings settings)
        {
            var model = MiniMapper.Map<InvoicePaymentSettings, InvoiceConfigurationModel>(settings);
            model.PostActionName = nameof(InvoiceConfigure);

            PrepareViewBag("Payments.Invoice");

            return View("GenericConfigure", model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult InvoiceConfigure(InvoiceConfigurationModel model, InvoicePaymentSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return InvoiceConfigure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(InvoiceConfigure));
        }

        #endregion

        #region PayInStore

        [LoadSetting, AuthorizeAdmin]
        public IActionResult PayInStoreConfigure(PayInStorePaymentSettings settings)
        {
            var model = MiniMapper.Map<PayInStorePaymentSettings, PayInStoreConfigurationModel>(settings);
            model.PostActionName = nameof(PayInStoreConfigure);

            PrepareViewBag("Payments.PayInStore");

            return View("GenericConfigure", model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult PayInStoreConfigure(PayInStoreConfigurationModel model, PayInStorePaymentSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return PayInStoreConfigure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(PayInStoreConfigure));
        }

        #endregion

        #region Prepayment

        [LoadSetting, AuthorizeAdmin]
        public IActionResult PrepaymentConfigure(PrepaymentPaymentSettings settings)
        {
            var model = MiniMapper.Map<PrepaymentPaymentSettings, PrepaymentConfigurationModel>(settings);
            model.PostActionName = nameof(PrepaymentConfigure);

            PrepareViewBag("Payments.Prepayment");

            return View("GenericConfigure", model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult PrepaymentConfigure(PrepaymentConfigurationModel model, PrepaymentPaymentSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return PrepaymentConfigure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(PrepaymentConfigure));
        }

        #endregion

        #region DirectDebit

        [LoadSetting, AuthorizeAdmin]
        public IActionResult DirectDebitConfigure(DirectDebitPaymentSettings settings)
        {
            var model = MiniMapper.Map<DirectDebitPaymentSettings, DirectDebitConfigurationModel>(settings);
            model.PostActionName = nameof(DirectDebitConfigure);

            PrepareViewBag("Payments.DirectDebit");

            return View("GenericConfigure", model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult DirectDebitConfigure(DirectDebitConfigurationModel model, DirectDebitPaymentSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return DirectDebitConfigure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(DirectDebitConfigure));
        }

        #endregion

        #region PurchaseOrderNumber

        [LoadSetting, AuthorizeAdmin]
        public IActionResult PurchaseOrderNumberConfigure(PurchaseOrderNumberPaymentSettings settings)
        {
            var model = MiniMapper.Map<PurchaseOrderNumberPaymentSettings, PurchaseOrderNumberConfigurationModel>(settings);
            model.PostActionName = nameof(PurchaseOrderNumberConfigure);

            PrepareViewBag("Payments.PurchaseOrderNumber");

            return View("GenericConfigure", model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult PurchaseOrderNumberConfigure(PurchaseOrderNumberConfigurationModel model, PurchaseOrderNumberPaymentSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return PurchaseOrderNumberConfigure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(PurchaseOrderNumberConfigure));
        }

        #endregion

        #region Manual

        [LoadSetting, AuthorizeAdmin]
        public IActionResult ManualConfigure(ManualPaymentSettings settings)
        {
            var model = MiniMapper.Map<ManualPaymentSettings, ManualConfigurationModel>(settings);
            model.PostActionName = nameof(ManualConfigure);
            model.ExcludedCreditCards = settings.ExcludedCreditCards.SplitSafe(',').ToArray();
            model.AvailableCreditCards = ManualProvider.GetCreditCardBrands(T)
                .Select(x => new SelectListItem
                {
                    Text = x.Value,
                    Value = x.Key,
                    Selected = model.ExcludedCreditCards.Contains(x.Key)
                })
                .ToList();

            PrepareViewBag("Payments.Manual");

            return View(nameof(ManualConfigure), model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult ManualConfigure(ManualConfigurationModel model, ManualPaymentSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return ManualConfigure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);
            settings.ExcludedCreditCards = string.Join(',', model.ExcludedCreditCards ?? []);

            return RedirectToAction(nameof(ManualConfigure));
        }

        #endregion

        private void PrepareViewBag(string providerSystemName)
        {
            ViewBag.Provider = _providerManager.GetProvider(providerSystemName).Metadata;
            ViewBag.PrimaryStoreCurrencyCode = Services.CurrencyService.PrimaryCurrency.CurrencyCode;
            ViewBag.TransactModes = new List<SelectListItem>
            {
                new() { Text = T("Enums.PaymentStatus.Pending"), Value = ((int)TransactMode.Pending).ToString() },
                new() { Text = T("Enums.PaymentStatus.Authorized"), Value = ((int)TransactMode.Authorize).ToString() },
                new() { Text = T("Enums.PaymentStatus.Paid"), Value = ((int)TransactMode.Paid).ToString() }
            };
        }
    }
}
