using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Caching;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.Pdf;

namespace Smartstore.Web.Controllers
{
    public class PdfController : Controller
    {
        private readonly ISettingFactory _settingFactory;
        private readonly ICacheFactory _cacheFactory;
        private readonly IStoreContext _storeContext;
        private readonly IAddressService _addressService;

        public PdfController(ISettingFactory settingFactory, ICacheFactory cacheFactory, IStoreContext storeContext, IAddressService addressService)
        {
            _settingFactory = settingFactory;
            _cacheFactory = cacheFactory;
            _storeContext = storeContext;
            _addressService = addressService;
        }

        [NeverAuthorize]
        public async Task<IActionResult> ReceiptHeader(PdfSectionVariables vars, int storeId = 0, bool isPartial = false)
        {
            var model = await PreparePdfReceiptSectionModelAsync(storeId);
            model.Variables = vars;

            ViewBag.IsPartial = isPartial;

            if (isPartial)
                return PartialView(model);
            return View(model);
        }

        [NeverAuthorize]
        public async Task<IActionResult> ReceiptFooter(PdfSectionVariables vars, int storeId = 0, bool isPartial = false)
        {
            var model = await PreparePdfReceiptSectionModelAsync(storeId);
            model.Variables = vars;

            ViewBag.IsPartial = isPartial;

            if (isPartial)
                return PartialView(model);
            return View(model);
        }

        protected async Task<PdfReceiptSectionModel> PreparePdfReceiptSectionModelAsync(int storeId)
        {
            return await _cacheFactory.GetMemoryCache().GetAsync("PdfReceiptSectionModel-{0}".FormatInvariant(storeId), async (o) =>
            {
                // 1 min. (just for the duration of pdf processing)
                o.ExpiresIn(TimeSpan.FromMinutes(1));

                var model = new PdfReceiptSectionModel { StoreId = storeId };
                var store = _storeContext.GetStoreById(model.StoreId) ?? _storeContext.CurrentStore;

                var companyInfoSettings = await _settingFactory.LoadSettingsAsync<CompanyInformationSettings>(store.Id);
                var bankSettings = await _settingFactory.LoadSettingsAsync<BankConnectionSettings>(store.Id);
                var contactSettings = await _settingFactory.LoadSettingsAsync<ContactDataSettings>(store.Id);
                var pdfSettings = await _settingFactory.LoadSettingsAsync<PdfSettings>(store.Id);

                model.StoreName = store.Name;
                model.StoreUrl = store.Url;
                model.LogoId = pdfSettings.LogoPictureId != 0 ? pdfSettings.LogoPictureId : store.LogoMediaFileId;

                model.MerchantCompanyInfo = companyInfoSettings;
                model.MerchantBankAccount = bankSettings;
                model.MerchantContactData = contactSettings;
                model.MerchantFormattedAddress = await _addressService.FormatAddressAsync(companyInfoSettings, true);

                return model;
            });
        }
    }
}
