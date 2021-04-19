using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging.Events;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.Messaging
{
    public partial class MessageModelProvider
    {
        private void ApplyCustomerContentPart(IDictionary<string, object> model, CustomerContent content, MessageContext ctx)
        {
            model["CustomerId"] = content.CustomerId;
            model["IpAddress"] = content.IpAddress;
            model["CreatedOn"] = ToUserDate(content.CreatedOnUtc, ctx);
            model["UpdatedOn"] = ToUserDate(content.UpdatedOnUtc, ctx);
        }

        private static string BuildUrl(string url, MessageContext ctx)
        {
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + url.EnsureStartsWith("/");
        }

        private string BuildRouteUrl(object routeValues, MessageContext ctx)
        {
            // TODO: (mh) (core) Test if URL resolution works correctly and ensure that routes did not change.
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper?.RouteUrl(routeValues);
        }

        private string BuildRouteUrl(string routeName, object routeValues, MessageContext ctx)
        {
            // TODO: (mh) (core) Test if URL resolution works correctly and ensure that routes did not change.
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper?.RouteUrl(routeName, routeValues);
        }

        private string BuildActionUrl(string action, string controller, object routeValues, MessageContext ctx)
        {
            // TODO: (mh) (core) Test if URL resolution works correctly and ensure that routes did not change.
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper?.Action(action, controller, routeValues);
        }

        private async Task PublishModelPartCreatedEventAsync<T>(T source, dynamic part) where T : class
        {
            await _services.EventPublisher.PublishAsync(new MessageModelPartCreatedEvent<T>(source, part));
        }

        private string GetLocalizedValue(MessageContext messageContext, ProviderMetadata metadata, string propertyName, Func<ProviderMetadata, string> fallback)
        {
            // TODO: (mc) this actually belongs to PluginMediator, but we simply cannot add a dependency to framework from here. Refactor later!

            Guard.NotNull(metadata, nameof(metadata));

            var resourceName = metadata.ResourceKeyPattern.FormatInvariant(metadata.SystemName, propertyName);
            string result = _localizationService.GetResource(resourceName, messageContext.Language.Id, false, "", true);

            if (result.IsEmpty())
                result = fallback(metadata);

            return result;
        }

        private async Task<object> GetTopicAsync(string topicSystemName, MessageContext ctx)
        {
            var topic = await _db.Topics
                .AsNoTracking()
                .ApplyStoreFilter(ctx.StoreId ?? 0)
                .FirstOrDefaultAsync(x => x.SystemName == topicSystemName);

            string body = topic?.GetLocalized(x => x.Body, ctx.Language);
            if (body.HasValue())
            {
                body = HtmlUtils.RelativizeFontSizes(body);
            }

            return new
            {
                Title = topic?.GetLocalized(x => x.Title, ctx.Language).Value.NullEmpty(),
                Body = body.NullEmpty()
            };
        }

        private static string GetDisplayNameForCustomer(Customer customer)
        {
            return customer.GetFullName().NullEmpty() ?? customer.Username ?? customer.FindEmail();
        }

        private string GetBoolResource(bool value, MessageContext ctx)
        {
            return _localizationService.GetResource(value ? "Common.Yes" : "Common.No", ctx.Language.Id);
        }

        private DateTime? ToUserDate(DateTime? utcDate, MessageContext messageContext)
        {
            if (utcDate == null)
                return null;

            return _services.DateTimeHelper.ConvertToUserTime(
                utcDate.Value,
                TimeZoneInfo.Utc,
                _services.DateTimeHelper.GetCustomerTimeZone(messageContext.Customer));
        }

        private Money FormatPrice(decimal price, Order order, MessageContext messageContext)
        {
            return FormatPrice(price, order.CustomerCurrencyCode, messageContext, order.CurrencyRate);
        }

        private Money FormatPrice(decimal price, MessageContext messageContext, decimal exchangeRate = 1)
        {
            return FormatPrice(price, (Currency)null, messageContext, exchangeRate);
        }

        private Money FormatPrice(decimal price, string currencyCode, MessageContext messageContext, decimal exchangeRate = 1)
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

        private Money FormatPrice(decimal price, Currency currency, MessageContext messageContext, decimal exchangeRate = 1)
        {
            currency ??= _services.Resolve<IWorkContext>().WorkingCurrency;

            if (exchangeRate != 1)
            {
                return new(price * exchangeRate, currency);
            }

            return new(price, currency);
        }

        private async Task<MediaFileInfo> GetMediaFileFor(Product product, ProductVariantAttributeSelection attrSelection = null)
        {
            var attrParser = _services.Resolve<IProductAttributeMaterializer>();
            var mediaService = _services.Resolve<IMediaService>();

            MediaFileInfo file = null;

            if (attrSelection != null)
            {
                var combination = await attrParser.FindAttributeCombinationAsync(product.Id, attrSelection);

                if (combination != null)
                {
                    var fileIds = combination.GetAssignedMediaIds();
                    if (fileIds?.Any() ?? false)
                    {
                        file = await mediaService.GetFileByIdAsync(fileIds[0], MediaLoadFlags.AsNoTracking);
                    }
                }
            }

            if (file == null)
            {
                file = await mediaService.GetFileByIdAsync(product.MainPictureId ?? 0, MediaLoadFlags.AsNoTracking);
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
                    file = mediaService.ConvertMediaFile(productFile.MediaFile);
                }
            }

            return file;
        }

        private static object[] Concat(params object[] values)
        {
            return values.Where(x => CommonHelper.IsTruthy(x)).ToArray();
        }
    }
}
