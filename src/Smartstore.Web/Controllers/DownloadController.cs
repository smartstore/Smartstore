using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Seo;
using Smartstore.Utilities.Html;

namespace Smartstore.Web.Controllers
{
    public class DownloadController : SmartController
    {
        private readonly SmartDbContext _db;
        private readonly IDownloadService _downloadService;
        private readonly CustomerSettings _customerSettings;

        public DownloadController(
            SmartDbContext db,
            IDownloadService downloadService,
            CustomerSettings customerSettings)
        {
            _db = db;
            _downloadService = downloadService;
            _customerSettings = customerSettings;
        }

        [NonAction]
        private IActionResult GetFileStreamResultFor(Download download, Stream stream)
        {
            if (stream == null || stream.Length == 0)
            {
                NotifyError(T("Common.Download.NoDataAvailable"));
                return new RedirectResult(Url.Action("Info", "Customer"));
            }

            var fileName = download.MediaFile.Name;
            var contentType = download.MediaFile.MimeType;

            return new FileStreamResult(stream, contentType)
            {
                FileDownloadName = fileName
            };
        }

        [NonAction]
        private async Task<IActionResult> GetFileStreamResultForAsync(Download download)
        {
            return GetFileStreamResultFor(download, await _downloadService.OpenDownloadStreamAsync(download));
        }

        public async Task<IActionResult> Sample(int productId)
        {
            var product = await _db.Products.FindByIdAsync(productId, false);
                
            if (product == null)
            {
                return NotFound();
            }
            
            if (!product.HasSampleDownload)
            {
                NotifyError(T("Common.Download.HasNoSample"));
                return RedirectToRoute("Product", new { SeName = await product.GetActiveSlugAsync() });
            }

            var download = await _db.Downloads
                .Include(x => x.MediaFile)
                .FindByIdAsync(product.SampleDownloadId.GetValueOrDefault(), false);
                
            if (download == null)
            {
                NotifyError(T("Common.Download.SampleNotAvailable"));
                return RedirectToRoute("Product", new { SeName = await product.GetActiveSlugAsync() });
            }

            if (download.UseDownloadUrl)
            {
                return new RedirectResult(download.DownloadUrl);
            }
            
            return await GetFileStreamResultForAsync(download);
        }

        public async Task<IActionResult> GetDownload(Guid id, bool agree = false, string fileVersion = "")
        {
            if (id == Guid.Empty)
            {
                return NotFound();
            }

            var orderItem = await _db.OrderItems
                .Include(x => x.Product)
                .Include(x => x.Order)
                .Where(x => x.OrderItemGuid == id)
                .FirstOrDefaultAsync();

            if (orderItem == null)
            {
                return NotFound();
            }
            
            var order = orderItem.Order;
            var product = orderItem.Product;
            var hasNotification = false;

            if (!_downloadService.IsDownloadAllowed(orderItem))
            {
                hasNotification = true;
                NotifyError(T("Common.Download.NotAllowed"));
            }

            if (_customerSettings.DownloadableProductsValidateUser)
            {
                var customer = Services.WorkContext.CurrentCustomer;
                if (customer == null)
                {
                    return new UnauthorizedResult();
                }
                
                if (order.CustomerId != customer.Id)
                {
                    hasNotification = true;
                    NotifyError(T("Account.CustomerOrders.NotYourOrder"));
                }
            }

            Download download;

            if (fileVersion.HasValue())
            {
                download = await _db.Downloads
                    .AsNoTracking()
                    .ApplyEntityFilter(product)
                    .ApplyVersionFilter(fileVersion)
                    .Include(x => x.MediaFile)
                    .FirstOrDefaultAsync();
            }
            else
            {
                download = (await _db.Downloads
                    .AsNoTracking()
                    .ApplyEntityFilter(nameof(product), product.Id)
                    .Include(x => x.MediaFile)
                    .ToListAsync())
                    .OrderByVersion()
                    .FirstOrDefault();
            }

            if (download == null)
            {
                hasNotification = true;
                NotifyError(T("Common.Download.NoDataAvailable"));
            }

            if (product.HasUserAgreement && !agree)
            {
                hasNotification = true;
            }

            if (!product.UnlimitedDownloads && orderItem.DownloadCount >= product.MaxNumberOfDownloads)
            {
                hasNotification = true;
                NotifyError(T("Common.Download.MaxNumberReached", product.MaxNumberOfDownloads));
            }

            if (hasNotification)
            {
                return RedirectToAction("UserAgreement", "Customer", new { id, fileVersion });
            }

            if (download.UseDownloadUrl)
            {
                orderItem.DownloadCount++;
                await _db.SaveChangesAsync();
                
                return new RedirectResult(download.DownloadUrl);
            }
            else
            {
                var stream = await _downloadService.OpenDownloadStreamAsync(download);
                if (stream == null || stream.Length == 0)
                {
                    NotifyError(T("Common.Download.NoDataAvailable"));
                    return RedirectToAction("UserAgreement", "Customer", new { id });
                }

                // TODO: (mh) (core) Why did you close the stream? Because now the result cannot be executed, it needs an open stream and will close it after done executing.
                // Did you observe a mem leak?
                //stream.Close();

                orderItem.DownloadCount++;
                await _db.SaveChangesAsync();

                return GetFileStreamResultFor(download, stream);
            }
        }

        public async Task<IActionResult> GetLicense(Guid id)
        {
            if (id == Guid.Empty)
            {
                return NotFound();
            }

            var orderItem = await _db.OrderItems
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.Order)
                .Where(x => x.OrderItemGuid == id)
                .FirstOrDefaultAsync();

            if (orderItem == null)
            {
                return NotFound();
            }

            var order = orderItem.Order;
            var product = orderItem.Product;

            if (!_downloadService.IsLicenseDownloadAllowed(orderItem))
            {
                NotifyError(T("Common.Download.NotAllowed"));
                return RedirectToAction("DownloadableProducts", "Customer");
            }

            if (_customerSettings.DownloadableProductsValidateUser)
            {
                var customer = Services.WorkContext.CurrentCustomer;
                if (customer == null)
                {
                    return new UnauthorizedResult();
                }

                if (order.CustomerId != customer.Id)
                {
                    NotifyError(T("Account.CustomerOrders.NotYourOrder"));
                    return RedirectToAction("DownloadableProducts", "Customer");
                }
            }

            var download = await _db.Downloads
                .Include(x => x.MediaFile)
                .FindByIdAsync(orderItem.LicenseDownloadId ?? 0, false);

            if (download == null)
            {
                NotifyError(T("Common.Download.NotAvailable"));
                return RedirectToAction("DownloadableProducts", "Customer");
            }

            if (download.UseDownloadUrl)
            {
                return new RedirectResult(download.DownloadUrl);
            }
            
            return await GetFileStreamResultForAsync(download);
        }

        public async Task<IActionResult> GetFileUpload(Guid downloadId)
        {
            var download = await _db.Downloads
                .AsNoTracking()
                .Where(x => x.DownloadGuid == downloadId)
                .FirstOrDefaultAsync();
                
            if (download == null)
            {
                NotifyError(T("Common.Download.NotAvailable"));
                return RedirectToAction("DownloadableProducts", "Customer");
            }

            if (download.UseDownloadUrl)
            {
                return new RedirectResult(download.DownloadUrl);
            }

            return await GetFileStreamResultForAsync(download);
        }

        public async Task<IActionResult> GetUserAgreement(int productId, bool? asPlainText)
        {
            var product = await _db.Products.FindByIdAsync(productId, false);
                
            if (product == null)
            {
                return Content(T("Products.NotFound", productId));
            }
            
            if (!product.IsDownload || !product.HasUserAgreement || product.UserAgreementText.IsEmpty())
            {
                return Content(T("DownloadableProducts.HasNoUserAgreement"));
            }
            
            if (asPlainText ?? false)
            {
                var agreement = HtmlUtils.ConvertHtmlToPlainText(product.UserAgreementText);
                agreement = HtmlUtils.StripTags(HttpUtility.HtmlDecode(agreement));

                return Content(agreement);
            }

            return Content(product.UserAgreementText);
        }
    }
}
