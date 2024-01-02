using System.Web;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Seo;
using Smartstore.Utilities.Html;

namespace Smartstore.Web.Controllers
{
    public class DownloadController : PublicController
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

        // INFO: overwriting 'SaveChanges' property at class level has no effect.
        [SaveChanges<SmartDbContext>(false)]
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

            var result = await GetResultFor(download);
            if (result != null)
            {
                return result;
            }

            NotifyError(T("Common.Download.SampleNotAvailable"));
            return RedirectToRoute("Product", new { SeName = await product.GetActiveSlugAsync() });
        }

        [SaveChanges<SmartDbContext>(false)]
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
            var errors = new List<string>();
            Download download;

            if (!_downloadService.IsDownloadAllowed(orderItem))
            {
                errors.Add(T("Common.Download.NotAllowed"));
            }

            if (_customerSettings.DownloadableProductsValidateUser)
            {
                var customer = Services.WorkContext.CurrentCustomer;
                if (customer == null)
                {
                    return ChallengeOrForbid();
                }

                if (order.CustomerId != customer.Id)
                {
                    errors.Add(T("Account.CustomerOrders.NotYourOrder"));
                }
            }

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
                errors.Add(T("Common.Download.NoDataAvailable"));
            }

            if (!product.UnlimitedDownloads && orderItem.DownloadCount >= product.MaxNumberOfDownloads)
            {
                errors.Add(T("Common.Download.MaxNumberReached", product.MaxNumberOfDownloads));
            }

            if (errors.Count > 0)
            {
                errors.Each(x => NotifyError(x));
            }

            if (errors.Count > 0 || (product.HasUserAgreement && !agree))
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
                var mediaFile = download.MediaFile;
                if (mediaFile == null || mediaFile.Size == 0)
                {
                    NotifyError(T("Common.Download.NoDataAvailable"));
                    return RedirectToAction("UserAgreement", "Customer", new { id });
                }

                orderItem.DownloadCount++;
                await _db.SaveChangesAsync();

                return await GetFileStreamResultFor(download);
            }
        }

        [SaveChanges<SmartDbContext>(false)]
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
                    return ChallengeOrForbid();
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

            var result = await GetResultFor(download);
            if (result != null)
            {
                return result;
            }

            NotifyError(T("Common.Download.NotAvailable"));
            return RedirectToAction("DownloadableProducts", "Customer");
        }

        [SaveChanges<SmartDbContext>(false)]
        public async Task<IActionResult> GetFileUpload(Guid downloadId)
        {
            var download = await _db.Downloads
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .Where(x => x.DownloadGuid == downloadId)
                .FirstOrDefaultAsync();

            var result = await GetResultFor(download);
            if (result != null)
            {
                return result;
            }

            NotifyError(T("Common.Download.NotAvailable"));
            return RedirectToAction("DownloadableProducts", "Customer");
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
                var agreement = HtmlUtility.ConvertHtmlToPlainText(product.UserAgreementText);
                agreement = HtmlUtility.StripTags(HttpUtility.HtmlDecode(agreement));

                return Content(agreement);
            }

            return Content(product.UserAgreementText);
        }

        private async Task<IActionResult> GetResultFor(Download download)
        {
            if (download != null)
            {
                if (download.UseDownloadUrl)
                {
                    return new RedirectResult(download.DownloadUrl);
                }
                else if (download.MediaFile != null)
                {
                    return await GetFileStreamResultFor(download);
                }
            }

            return null;
        }

        private async Task<IActionResult> GetFileStreamResultFor(Download download)
        {
            var stream = await _downloadService.OpenDownloadStreamAsync(download);

            if (stream == null || stream.Length == 0)
            {
                NotifyError(T("Common.Download.NoDataAvailable"));
                return RedirectToAction("Info", "Customer");
            }

            return new FileStreamResult(stream, download.MediaFile.MimeType)
            {
                FileDownloadName = download.MediaFile.Name
            };
        }
    }
}
