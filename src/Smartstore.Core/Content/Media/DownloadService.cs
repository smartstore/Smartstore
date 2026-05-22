using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;

namespace Smartstore.Core.Content.Media;

public partial class DownloadService : IDownloadService
{
    private readonly SmartDbContext _db;
    private readonly IMediaService _mediaService;

    public DownloadService(SmartDbContext db, IMediaService mediaService)
    {
        _db = db;
        _mediaService = mediaService;
    }

    public virtual async Task<MediaFileInfo> InsertDownloadAsync(Download download, Stream stream, string fileName)
    {
        Guard.NotNull(download);
        Guard.NotEmpty(fileName);

        var path = _mediaService.CombinePaths(SystemAlbumProvider.Downloads, fileName);
        var file = await _mediaService.SaveFileAsync(path, stream, dupeFileHandling: DuplicateFileHandling.Rename);
        file.File.Hidden = true;
        download.MediaFile = file.File;

        _db.Downloads.Add(download);
        await _db.SaveChangesAsync();

        return file;
    }

    public virtual bool IsDownloadAllowed(OrderItem orderItem)
    {
        var order = orderItem?.Order;
        var product = orderItem?.Product;

        if (order == null
            || order.Deleted
            || order.OrderStatus == OrderStatus.Cancelled
            || product == null
            || !product.IsDownload)
        {
            return false;
        }

        // Check payment status
        if (product.DownloadActivationType == DownloadActivationType.WhenOrderIsPaid
            && order.PaymentStatus == PaymentStatus.Paid
            && order.PaidDateUtc.HasValue)
        {
            return CheckExpiration(order.PaidDateUtc.Value);
        }
        else if (product.DownloadActivationType == DownloadActivationType.Manually && orderItem.IsDownloadActivated)
        {
            return CheckExpiration(order.CreatedOnUtc);
        }

        return false;

        bool CheckExpiration(DateTime date)
        {
            return product.DownloadExpirationDays == null || date.AddDays(product.DownloadExpirationDays.Value) > DateTime.UtcNow;
        }
    }

    public virtual Stream OpenDownloadStream(Download download)
    {
        Guard.NotNull(download);
        return _mediaService.StorageProvider.OpenRead(download.MediaFile);
    }

    public virtual Task<Stream> OpenDownloadStreamAsync(Download download)
    {
        Guard.NotNull(download);
        return _mediaService.StorageProvider.OpenReadAsync(download.MediaFile);
    }
}