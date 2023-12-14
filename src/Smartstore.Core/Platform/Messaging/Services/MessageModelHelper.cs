using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging.Events;
using Smartstore.Core.Stores;
using Smartstore.Events;
using Smartstore.Utilities;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.Messaging
{
    public partial class MessageModelHelper
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILocalizationService _localizationService;
        private readonly IMediaService _mediaService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly Lazy<IUrlHelper> _urlHelper;

        public MessageModelHelper(
            SmartDbContext db,
            IWorkContext workContext,
            IDateTimeHelper dateTimeHelper,
            IEventPublisher eventPublisher,
            ILocalizationService localizationService,
            IMediaService mediaService,
            IProductAttributeMaterializer productAttributeMaterializer,
            Lazy<IUrlHelper> urlHelper)
        {
            _db = db;
            _workContext = workContext;
            _dateTimeHelper = dateTimeHelper;
            _eventPublisher = eventPublisher;
            _localizationService = localizationService;
            _mediaService = mediaService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _urlHelper = urlHelper;
        }

        public void ApplyCustomerContentPart(IDictionary<string, object> model, CustomerContent content, MessageContext ctx)
        {
            model["CustomerId"] = content.CustomerId;
            model["IpAddress"] = content.IpAddress;
            model["CreatedOn"] = ToUserDate(content.CreatedOnUtc, ctx);
            model["UpdatedOn"] = ToUserDate(content.UpdatedOnUtc, ctx);
        }

        public string BuildUrl(string url, MessageContext ctx)
        {
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + url.EmptyNull().EnsureStartsWith('/');
        }

        public string BuildRouteUrl(object routeValues, MessageContext ctx)
        {
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper.Value?.RouteUrl(routeValues);
        }

        public string BuildRouteUrl(string routeName, object routeValues, MessageContext ctx)
        {
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper.Value?.RouteUrl(routeName, routeValues);
        }

        public string BuildActionUrl(string action, string controller, object routeValues, MessageContext ctx)
        {
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper.Value?.Action(action, controller, routeValues);
        }

        public async Task PublishModelPartCreatedEventAsync<T>(T source, dynamic part) where T : class
        {
            await _eventPublisher.PublishAsync(new MessageModelPartCreatedEvent<T>(source, part));
        }

        public async Task<object> GetTopicAsync(string topicSystemName, MessageContext ctx)
        {
            var topic = await _db.Topics
                .AsNoTracking()
                .ApplyStoreFilter(ctx.StoreId ?? 0)
                .FirstOrDefaultAsync(x => x.SystemName == topicSystemName);

            string body = topic?.GetLocalized(x => x.Body, ctx.Language);
            if (body.HasValue())
            {
                body = HtmlUtility.RelativizeFontSizes(body);
            }

            return new
            {
                Title = topic?.GetLocalized(x => x.Title, ctx.Language).Value.NullEmpty(),
                Body = body.NullEmpty()
            };
        }

        public string GetBoolResource(bool value, MessageContext ctx)
        {
            return _localizationService.GetResource(value ? "Common.Yes" : "Common.No", ctx.Language.Id);
        }

        public DateTime? ToUserDate(DateTime? utcDate, MessageContext messageContext)
        {
            if (utcDate == null)
                return null;

            return _dateTimeHelper.ConvertToUserTime(
                utcDate.Value,
                TimeZoneInfo.Utc,
                _dateTimeHelper.GetCustomerTimeZone(messageContext.Customer));
        }

        public Money FormatPrice(decimal price, Order order, MessageContext messageContext)
        {
            return FormatPrice(price, order.CustomerCurrencyCode, messageContext, order.CurrencyRate);
        }

        public Money FormatPrice(decimal price, MessageContext messageContext, decimal exchangeRate = 1)
        {
            return FormatPrice(price, (Currency)null, messageContext, exchangeRate);
        }

        public Money FormatPrice(decimal price, string currencyCode, MessageContext messageContext, decimal exchangeRate = 1)
        {
            // Currencies are cached, so no need for async in this simple case.
            var currency = _db.Currencies
                .AsNoTracking()
                .Where(x => x.CurrencyCode == currencyCode)
                .FirstOrDefault();

            return FormatPrice(
                price,
                currency ?? new Currency { CurrencyCode = currencyCode },
                messageContext,
                exchangeRate);
        }

        public Money FormatPrice(decimal price, Currency currency, MessageContext messageContext, decimal exchangeRate = 1)
        {
            currency ??= _workContext.WorkingCurrency;

            if (exchangeRate != 1)
            {
                return new(price * exchangeRate, currency);
            }

            return new(price, currency);
        }

        public async Task<MediaFileInfo> GetMediaFileFor(Product product, ProductVariantAttributeSelection attrSelection = null)
        {
            MediaFileInfo file = null;

            if (attrSelection != null)
            {
                var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, attrSelection);

                if (combination != null)
                {
                    var fileIds = combination.GetAssignedMediaIds();
                    if (fileIds?.Any() ?? false)
                    {
                        file = await _mediaService.GetFileByIdAsync(fileIds[0], MediaLoadFlags.AsNoTracking);
                    }
                }
            }

            if (file == null)
            {
                file = await _mediaService.GetFileByIdAsync(product.MainPictureId ?? 0, MediaLoadFlags.AsNoTracking);
            }

            if (file == null && product.Visibility == ProductVisibility.Hidden && product.ParentGroupedProductId > 0)
            {
                var productFile = await _db.ProductMediaFiles
                    .AsNoTracking()
                    .Include(x => x.MediaFile)
                    .ApplyProductFilter(product.ParentGroupedProductId)
                    .FirstOrDefaultAsync();

                if (productFile?.MediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(productFile.MediaFile);
                }
            }

            return file;
        }

        public static string GetDisplayNameForCustomer(Customer customer)
        {
            var fullName = 
                customer.GetFullName().NullEmpty() ?? 
                customer.Username ?? 
                customer.FindEmail();

            return fullName?
                .Replace(",", string.Empty)
                .Replace(";", string.Empty);
        }

        // INFO: parameters must be of type 'string'. Type 'object' outputs nothing.
        public static string[] GetValidValues(params string[] values)
            => values.Where(CommonHelper.IsTruthy).ToArray();
    }
}
