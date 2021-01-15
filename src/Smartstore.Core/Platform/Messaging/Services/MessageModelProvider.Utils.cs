using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Customers;
using Smartstore.Core.Messages.Events;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities;

namespace Smartstore.Core.Messages
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

        private string BuildUrl(string url, MessageContext ctx)
        {
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + url.EnsureStartsWith("/");
        }

        private string BuildRouteUrl(object routeValues, MessageContext ctx)
        {
            if (_urlHelper == null)
                return string.Empty;

            // TODO: (mh) (core) Test if this works.
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper.RouteUrl(new UrlRouteContext { Values = routeValues });
        }

        private string BuildRouteUrl(string routeName, object routeValues, MessageContext ctx)
        {
            if (_urlHelper == null)
                return string.Empty;

            // TODO: (mh) (core) Test if this works.
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper.RouteUrl(new UrlRouteContext { RouteName = routeName, Values = routeValues });
        }

        private string BuildActionUrl(string action, string controller, object routeValues, MessageContext ctx)
        {
            if (_urlHelper == null)
                return string.Empty;

            // TODO: (mh) (core) Test if this works.
            return ctx.BaseUri.GetLeftPart(UriPartial.Authority) + _urlHelper.Action(new UrlActionContext { Action = action, Controller = controller, Values = routeValues });
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

        // TODO: (mh) (core) make async
        private object GetTopic(string topicSystemName, MessageContext ctx)
        {
            // TODO: (mh) (core) Uncomment once topicservice is available.

            //var topicService = _services.Resolve<ITopicService>();

            //// Load by store
            //var topic = topicService.GetTopicBySystemName(topicSystemName, ctx.StoreId ?? 0, false);

            //string body = topic?.GetLocalized(x => x.Body, ctx.Language);
            //if (body.HasValue())
            //{
            //    body = HtmlUtils.RelativizeFontSizes(body);
            //}

            return new
            {
                //Title = topic?.GetLocalized(x => x.Title, ctx.Language).Value.NullEmpty(),
                //Body = body.NullEmpty()
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
            // Currency is cached, so no need for async in this simple case.
            var currency = _db.Currencies
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
            if (exchangeRate != 1)
            {
                price = _services.Resolve<ICurrencyService>().ConvertCurrency(price, exchangeRate);
            }

            if (currency == null)
            {
                currency = _services.Resolve<IWorkContext>().WorkingCurrency;
            }

            return new Money(price, currency);
        }

        private async Task<MediaFileInfo> GetMediaFileForAsync(Product product, ProductVariantAttributeSelection attrSelection = null)
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
                    .ApplyProductFilter(new int[product.ParentGroupedProductId], 1)
                    .FirstOrDefaultAsync();

                if (productFile != null)
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
