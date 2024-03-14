using System.Reflection;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.DataExchange.Export.Events;
using Smartstore.Core.DataExchange.Export.Internal;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Stores;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExporter
    {
        private readonly static string[] _orderCustomerAttributes =
        [
            SystemCustomerAttributeNames.VatNumber,
            SystemCustomerAttributeNames.ImpersonatedCustomerId
        ];

        private async Task<IEnumerable<dynamic>> Convert(Order order, DataExporterContext ctx)
        {
            var result = new List<dynamic>();

            ctx.OrderBatchContext.Addresses.Collect(order.ShippingAddressId ?? 0);
            if (order.BillingAddressId.HasValue)
            {
                await ctx.OrderBatchContext.Addresses.GetOrLoadAsync(order.BillingAddressId.Value);
            }

            var customers = await ctx.OrderBatchContext.Customers.GetOrLoadAsync(order.CustomerId);
            var customer = customers.FirstOrDefault(x => x.Id == order.CustomerId);
            var orderItems = await ctx.OrderBatchContext.OrderItems.GetOrLoadAsync(order.Id);
            var shipments = await ctx.OrderBatchContext.Shipments.GetOrLoadAsync(order.Id);

            dynamic dynObject = ToDynamic(order, ctx);

            if (customer != null)
            {
                var genericAttributes = await ctx.OrderBatchContext.CustomerGenericAttributes.GetOrLoadAsync(order.CustomerId);
                var rewardPointsHistories = await ctx.OrderBatchContext.RewardPointsHistories.GetOrLoadAsync(order.CustomerId);

                dynObject.Customer = ToDynamic(customer);

                // We do not export all customer generic attributes because otherwise the export file gets too big.
                dynObject.Customer._GenericAttributes = genericAttributes
                    .Where(x => x.Value.HasValue() && _orderCustomerAttributes.Contains(x.Key))
                    .Select(x => CreateDynamic(x))
                    .ToList();

                dynObject.Customer.RewardPointsHistory = rewardPointsHistories
                    .Select(x => CreateDynamic(x))
                    .ToList();

                if (rewardPointsHistories.Any())
                {
                    dynObject.Customer._RewardPointsBalance = rewardPointsHistories
                        .OrderByDescending(x => x.CreatedOnUtc)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefault()
                        .PointsBalance;
                }
            }
            else
            {
                dynObject.Customer = null;
            }

            dynObject.BillingAddress = order.BillingAddressId.HasValue && ctx.OrderBatchContext.Addresses.TryGetValues(order.BillingAddressId.Value, out var billingAddresses)
                ? ToDynamic(billingAddresses.FirstOrDefault(), ctx)
                : null;

            dynObject.ShippingAddress = order.ShippingAddressId.HasValue && ctx.OrderBatchContext.Addresses.TryGetValues(order.ShippingAddressId.Value, out var shippingAddresses)
                ? ToDynamic(shippingAddresses.FirstOrDefault(), ctx)
                : null;

            dynObject.OrderItems = await orderItems
                .SelectAwait(async x => await ToDynamic(x, ctx))
                .AsyncToList();

            dynObject.Shipments = shipments
                .Select(x => ToDynamic(x, ctx))
                .ToList();

            result.Add(dynObject);

            await _services.EventPublisher.PublishAsync(new RowExportingEvent
            {
                Row = dynObject,
                EntityType = ExportEntityType.Order,
                ExportRequest = ctx.Request,
                ExecuteContext = ctx.ExecuteContext
            });

            return result;
        }

        private async Task<IEnumerable<dynamic>> Convert(Manufacturer manufacturer, DataExporterContext ctx)
        {
            var result = new List<dynamic>();
            var productManufacturers = await ctx.ManufacturerBatchContext.ProductManufacturers.GetOrLoadAsync(manufacturer.Id);

            dynamic dynObject = ToDynamic(manufacturer, ctx);

            if (manufacturer.MediaFileId.HasValue)
            {
                var numberOfFiles = ctx.Projection.NumberOfMediaFiles ?? int.MaxValue;
                var files = (await ctx.ManufacturerBatchContext.MediaFiles.GetOrLoadAsync(manufacturer.MediaFileId.Value)).Take(numberOfFiles);

                if (files.Any())
                {
                    dynObject.File = ToDynamic(files.First(), _mediaSettings.ManufacturerThumbPictureSize, _mediaSettings.ManufacturerThumbPictureSize, ctx);
                }
            }

            dynObject.ProductManufacturers = productManufacturers
                .OrderBy(x => x.DisplayOrder)
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x);
                    return dyn;
                })
                .ToList();

            result.Add(dynObject);

            await _services.EventPublisher.PublishAsync(new RowExportingEvent
            {
                Row = dynObject,
                EntityType = ExportEntityType.Manufacturer,
                ExportRequest = ctx.Request,
                ExecuteContext = ctx.ExecuteContext
            });

            return result;
        }

        private async Task<IEnumerable<dynamic>> Convert(Category category, DataExporterContext ctx)
        {
            var result = new List<dynamic>();
            var productCategories = await ctx.CategoryBatchContext.ProductCategories.GetOrLoadAsync(category.Id);

            dynamic dynObject = ToDynamic(category, ctx);

            if (category.MediaFileId.HasValue)
            {
                var numberOfFiles = ctx.Projection.NumberOfMediaFiles ?? int.MaxValue;
                var files = (await ctx.CategoryBatchContext.MediaFiles.GetOrLoadAsync(category.MediaFileId.Value)).Take(numberOfFiles);

                if (files.Any())
                {
                    dynObject.File = ToDynamic(files.First(), _mediaSettings.CategoryThumbPictureSize, _mediaSettings.CategoryThumbPictureSize, ctx);
                }
            }

            dynObject.ProductCategories = productCategories
                .OrderBy(x => x.DisplayOrder)
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x);
                    return dyn;
                })
                .ToList();

            result.Add(dynObject);

            await _services.EventPublisher.PublishAsync(new RowExportingEvent
            {
                Row = dynObject,
                EntityType = ExportEntityType.Category,
                ExportRequest = ctx.Request,
                ExecuteContext = ctx.ExecuteContext
            });

            return result;
        }

        private async Task<IEnumerable<dynamic>> Convert(Customer customer, DataExporterContext ctx)
        {
            var result = new List<dynamic>();
            var genericAttributes = await ctx.CustomerBatchContext.GenericAttributes.GetOrLoadAsync(customer.Id);

            dynamic dynObject = ToDynamic(customer);
            dynObject.BillingAddress = ToDynamic(customer.BillingAddress, ctx);
            dynObject.ShippingAddress = ToDynamic(customer.ShippingAddress, ctx);

            dynObject.Addresses = customer.Addresses
                .Select(x => ToDynamic(x, ctx))
                .ToList();

            dynObject._GenericAttributes = genericAttributes
                .Select(x => CreateDynamic(x))
                .ToList();

            dynObject._HasNewsletterSubscription = ctx.NewsletterSubscriptions.Contains(customer.Email, StringComparer.CurrentCultureIgnoreCase);
            dynObject._FullName = customer.GetFullName();
            dynObject._AvatarPictureUrl = null;

            if (_customerSettings.AllowCustomersToUploadAvatars)
            {
                // Reduce traffic and do not export default avatar.
                var fileId = genericAttributes.FirstOrDefault(x => x.Key == SystemCustomerAttributeNames.AvatarPictureId)?.Value?.ToInt() ?? 0;
                var file = await _mediaService.GetFileByIdAsync(fileId, MediaLoadFlags.AsNoTracking);
                if (file != null)
                {
                    dynObject._AvatarPictureUrl = _mediaService.GetUrl(file, new ProcessImageQuery { MaxSize = _mediaSettings.AvatarPictureSize }, ctx.Store.GetBaseUrl());
                }
            }

            result.Add(dynObject);

            await _services.EventPublisher.PublishAsync(new RowExportingEvent
            {
                Row = dynObject,
                EntityType = ExportEntityType.Customer,
                ExportRequest = ctx.Request,
                ExecuteContext = ctx.ExecuteContext
            });

            return result;
        }

        private async Task<IEnumerable<dynamic>> Convert(NewsletterSubscription subscription, DataExporterContext ctx)
        {
            var result = new List<dynamic>();
            dynamic dynObject = DataExporter.ToDynamic(subscription, ctx);
            result.Add(dynObject);

            await _services.EventPublisher.PublishAsync(new RowExportingEvent
            {
                Row = dynObject,
                EntityType = ExportEntityType.NewsletterSubscription,
                ExportRequest = ctx.Request,
                ExecuteContext = ctx.ExecuteContext
            });

            return result;
        }

        private async Task<IEnumerable<dynamic>> Convert(ShoppingCartItem shoppingCartItem, DataExporterContext ctx)
        {
            var result = new List<dynamic>();
            dynamic dynObject = await ToDynamic(shoppingCartItem, ctx);

            result.Add(dynObject);

            await _services.EventPublisher.PublishAsync(new RowExportingEvent
            {
                Row = dynObject,
                EntityType = ExportEntityType.ShoppingCartItem,
                ExportRequest = ctx.Request,
                ExecuteContext = ctx.ExecuteContext
            });

            return result;
        }

        private static dynamic ToDynamic(Currency currency, DataExporterContext ctx)
        {
            if (currency == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(currency);

            result.Name = ctx.GetTranslation(currency, nameof(currency.Name), currency.Name);
            result._Localized = GetLocalized(ctx, currency, x => x.Name);

            return result;
        }

        private static dynamic ToDynamic(Country country, DataExporterContext ctx)
        {
            if (country == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(country);

            result.Name = ctx.GetTranslation(country, nameof(country.Name), country.Name);
            result._Localized = GetLocalized(ctx, country, x => x.Name);

            return result;
        }

        private static dynamic ToDynamic(Address address, DataExporterContext ctx)
        {
            if (address == null)
            {
                return null;
            }

            var country = address.CountryId.HasValue
                ? ctx.Countries.Get(address.CountryId.Value)
                : null;

            var stateProvince = address.StateProvinceId.HasValue
                ? ctx.StateProvinces.Get(address.StateProvinceId.Value)
                : null;

            dynamic result = new DynamicEntity(address);

            result.Country = ToDynamic(country, ctx);

            if (stateProvince != null)
            {
                dynamic sp = new DynamicEntity(stateProvince);

                sp.Name = ctx.GetTranslation(stateProvince, nameof(stateProvince.Name), stateProvince.Name);
                sp._Localized = GetLocalized(ctx, stateProvince, x => x.Name);

                result.StateProvince = sp;
            }
            else
            {
                result.StateProvince = null;
            }

            return result;
        }

        private static dynamic ToDynamic(Customer customer)
        {
            if (customer == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(customer);

            result.BillingAddress = null;
            result.ShippingAddress = null;
            result.Addresses = null;

            result.RewardPointsHistory = null;
            result._RewardPointsBalance = 0;

            result._GenericAttributes = null;
            result._HasNewsletterSubscription = false;

            result._FullName = null;
            result._AvatarPictureUrl = null;

            result.CustomerRoles = customer.CustomerRoleMappings
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x.CustomerRole);
                    return dyn;
                })
                .ToList();

            return result;
        }

        private static dynamic ToDynamic(Store store)
        {
            if (store == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(store);
            return result;
        }

        private static dynamic ToDynamic(PriceLabel priceLabel, DataExporterContext ctx)
        {
            if (priceLabel == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(priceLabel);

            if (!ctx.IsPreview)
            {
                result.ShortName = ctx.GetTranslation(priceLabel, nameof(priceLabel.ShortName), priceLabel.ShortName);
                result.Name = ctx.GetTranslation(priceLabel, nameof(priceLabel.Name), priceLabel.Name);
                result.Description = ctx.GetTranslation(priceLabel, nameof(priceLabel.Description), priceLabel.Description);

                result._Localized = GetLocalized(ctx, priceLabel,
                    x => x.ShortName,
                    x => x.Name,
                    x => x.Description);
            }

            return result;
        }

        private static dynamic ToDynamic(DeliveryTime deliveryTime, DataExporterContext ctx)
        {
            if (deliveryTime == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(deliveryTime);

            result.Name = ctx.GetTranslation(deliveryTime, nameof(deliveryTime.Name), deliveryTime.Name);
            result._Localized = GetLocalized(ctx, deliveryTime, x => x.Name);

            return result;
        }

        private static dynamic ToDynamic(QuantityUnit quantityUnit, DataExporterContext ctx)
        {
            if (quantityUnit == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(quantityUnit);

            result.Name = ctx.GetTranslation(quantityUnit, nameof(quantityUnit.Name), quantityUnit.Name);
            result.NamePlural = ctx.GetTranslation(quantityUnit, nameof(quantityUnit.NamePlural), quantityUnit.NamePlural);
            result.Description = ctx.GetTranslation(quantityUnit, nameof(quantityUnit.Description), quantityUnit.Description);

            result._Localized = GetLocalized(ctx, quantityUnit,
                x => x.Name,
                x => x.NamePlural,
                x => x.Description);

            return result;
        }

        private static dynamic ToDynamic(Manufacturer manufacturer, DataExporterContext ctx)
        {
            if (manufacturer == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(manufacturer);

            result.File = null;
            result.Name = ctx.GetTranslation(manufacturer, nameof(manufacturer.Name), manufacturer.Name);

            if (!ctx.IsPreview)
            {
                result.SeName = ctx.GetUrlRecord(manufacturer);
                result.Description = ctx.GetTranslation(manufacturer, nameof(manufacturer.Description), manufacturer.Description);
                result.BottomDescription = ctx.GetTranslation(manufacturer, nameof(manufacturer.BottomDescription), manufacturer.BottomDescription);
                result.MetaKeywords = ctx.GetTranslation(manufacturer, nameof(manufacturer.MetaKeywords), manufacturer.MetaKeywords);
                result.MetaDescription = ctx.GetTranslation(manufacturer, nameof(manufacturer.MetaDescription), manufacturer.MetaDescription);
                result.MetaTitle = ctx.GetTranslation(manufacturer, nameof(manufacturer.MetaTitle), manufacturer.MetaTitle);

                result._Localized = GetLocalized(ctx, manufacturer,
                    x => x.Name,
                    x => x.Description,
                    x => x.BottomDescription,
                    x => x.MetaKeywords,
                    x => x.MetaDescription,
                    x => x.MetaTitle);
            }

            return result;
        }

        private static dynamic ToDynamic(Category category, DataExporterContext ctx)
        {
            if (category == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(category);

            result.File = null;
            result.Name = ctx.GetTranslation(category, nameof(category.Name), category.Name);
            result.FullName = ctx.GetTranslation(category, nameof(category.FullName), category.FullName);

            if (!ctx.IsPreview)
            {
                result.SeName = ctx.GetUrlRecord(category);
                result.Description = ctx.GetTranslation(category, nameof(category.Description), category.Description);
                result.BottomDescription = ctx.GetTranslation(category, nameof(category.BottomDescription), category.BottomDescription);
                result.MetaKeywords = ctx.GetTranslation(category, nameof(category.MetaKeywords), category.MetaKeywords);
                result.MetaDescription = ctx.GetTranslation(category, nameof(category.MetaDescription), category.MetaDescription);
                result.MetaTitle = ctx.GetTranslation(category, nameof(category.MetaTitle), category.MetaTitle);

                result._CategoryTemplateViewPath = ctx.CategoryTemplates.ContainsKey(category.CategoryTemplateId)
                    ? ctx.CategoryTemplates[category.CategoryTemplateId]
                    : string.Empty;

                result._Localized = GetLocalized(ctx, category,
                    x => x.Name,
                    x => x.FullName,
                    x => x.Description,
                    x => x.BottomDescription,
                    x => x.MetaKeywords,
                    x => x.MetaDescription,
                    x => x.MetaTitle);
            }

            return result;
        }

        private static dynamic ToDynamic(ProductVariantAttribute productAttribute, DataExporterContext ctx)
        {
            if (productAttribute == null)
            {
                return null;
            }

            var languageId = ctx.LanguageId;
            var attribute = productAttribute.ProductAttribute;

            dynamic result = new DynamicEntity(productAttribute);
            dynamic dynAttribute = new DynamicEntity(attribute);

            dynAttribute.Name = ctx.GetTranslation(attribute, nameof(attribute.Name), attribute.Name);
            dynAttribute.Description = ctx.GetTranslation(attribute, nameof(attribute.Description), attribute.Description);

            dynAttribute.Values = productAttribute.ProductVariantAttributeValues
                .OrderBy(x => x.DisplayOrder)
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x);
                    dyn.Name = ctx.GetTranslation(x, nameof(x.Name), x.Name);
                    dyn._Localized = GetLocalized(ctx, x, y => y.Name);

                    return dyn;
                })
                .ToList();

            dynAttribute._Localized = GetLocalized(ctx, attribute,
                x => x.Name,
                x => x.Description);

            result.Attribute = dynAttribute;

            return result;
        }

        private static dynamic ToDynamic(ProductVariantAttributeCombination attributeCombination, DataExporterContext ctx)
        {
            if (attributeCombination == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(attributeCombination);

            result.DeliveryTime = ctx.DeliveryTimes.TryGetValue(attributeCombination.DeliveryTimeId ?? 0, out var deliveryTime)
                ? ToDynamic(deliveryTime, ctx)
                : null;

            result.QuantityUnit = ctx.QuantityUnits.TryGetValue(attributeCombination.QuantityUnitId ?? 0, out var quantityUnit)
                ? ToDynamic(quantityUnit, ctx)
                : null;

            return result;
        }

        private static dynamic ToDynamic(ProductSpecificationAttribute productSpecificationAttribute, DataExporterContext ctx)
        {
            if (productSpecificationAttribute == null)
            {
                return null;
            }

            var option = productSpecificationAttribute.SpecificationAttributeOption;
            var attribute = option.SpecificationAttribute;

            dynamic result = new DynamicEntity(productSpecificationAttribute);
            dynamic dynAttribute = new DynamicEntity(attribute);

            dynAttribute.Name = ctx.GetTranslation(attribute, nameof(attribute.Name), attribute.Name);
            dynAttribute._Localized = GetLocalized(ctx, attribute, x => x.Name);

            dynAttribute.Alias = ctx.GetTranslation(attribute, nameof(attribute.Alias), attribute.Alias);
            dynAttribute._Localized = GetLocalized(ctx, attribute, x => x.Alias);

            dynamic dynOption = new DynamicEntity(option);

            dynOption.Name = ctx.GetTranslation(option, nameof(option.Name), option.Name);
            dynOption._Localized = GetLocalized(ctx, option, x => x.Name);

            dynOption.Alias = ctx.GetTranslation(option, nameof(option.Alias), option.Alias);
            dynOption._Localized = GetLocalized(ctx, option, x => x.Alias);

            dynOption.SpecificationAttribute = dynAttribute;
            result.SpecificationAttributeOption = dynOption;

            return result;
        }

        private dynamic ToDynamic(MediaFile file, int thumbPictureSize, int detailsPictureSize, DataExporterContext ctx)
        {
            if (file == null)
            {
                return null;
            }

            return ToDynamic(_mediaService.ConvertMediaFile(file), thumbPictureSize, detailsPictureSize, ctx);
        }

        private dynamic ToDynamic(MediaFileInfo file, int thumbPictureSize, int detailsPictureSize, DataExporterContext ctx)
        {
            if (file == null)
            {
                return null;
            }

            try
            {
                var host = ctx.Store.GetBaseUrl();
                dynamic result = new DynamicEntity(file.File);

                result._FileName = file.Name;
                result._RelativeUrl = _mediaService.GetUrl(file, null, null);
                result._ThumbImageUrl = _mediaService.GetUrl(file, thumbPictureSize, host);
                result._ImageUrl = _mediaService.GetUrl(file, detailsPictureSize, host);
                result._FullSizeImageUrl = _mediaService.GetUrl(file, null, host);

                return result;
            }
            catch (Exception ex)
            {
                ctx.Log.Error(ex, $"Failed to get details for file with ID {file.File.Id}.");
                return null;
            }
        }

        private dynamic ToDynamic(Order order, DataExporterContext ctx)
        {
            if (order == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(order);

            result.OrderNumber = order.GetOrderNumber();
            result.OrderStatus = _services.Localization.GetLocalizedEnum(order.OrderStatus, ctx.LanguageId);
            result.PaymentStatus = _services.Localization.GetLocalizedEnum(order.PaymentStatus, ctx.LanguageId);
            result.ShippingStatus = _services.Localization.GetLocalizedEnum(order.ShippingStatus, ctx.LanguageId);

            result.Customer = null;
            result.BillingAddress = null;
            result.ShippingAddress = null;
            result.Shipments = null;

            result.Store = ctx.Stores.ContainsKey(order.StoreId)
                ? DataExporter.ToDynamic(ctx.Stores[order.StoreId])
                : null;

            if (!ctx.IsPreview)
            {
                result.RedeemedRewardPointsEntry = CreateDynamic(order.RedeemedRewardPointsEntry);
            }

            return result;
        }

        private async Task<dynamic> ToDynamic(OrderItem orderItem, DataExporterContext ctx)
        {
            if (orderItem == null)
            {
                return null;
            }

            await _productAttributeMaterializer.MergeWithCombinationAsync(orderItem.Product, orderItem.AttributeSelection);

            dynamic result = new DynamicEntity(orderItem);
            result.Product = await ToDynamic(orderItem.Product, ctx);

            return result;
        }

        private static dynamic ToDynamic(Shipment shipment, DataExporterContext ctx)
        {
            if (shipment == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(shipment);

            result.ShipmentItems = shipment.ShipmentItems
                .Select(x =>
                {
                    dynamic exp = new DynamicEntity(x);
                    return exp;
                })
                .ToList();

            return result;
        }

        private static dynamic ToDynamic(NewsletterSubscription subscription, DataExporterContext ctx)
        {
            if (subscription == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(subscription);

            result.Store = ctx.Stores.ContainsKey(subscription.StoreId)
                ? DataExporter.ToDynamic(ctx.Stores[subscription.StoreId])
                : null;

            return result;
        }

        private async Task<dynamic> ToDynamic(ShoppingCartItem cartItem, DataExporterContext ctx)
        {
            if (cartItem == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(cartItem);

            await _productAttributeMaterializer.MergeWithCombinationAsync(cartItem.Product, cartItem.AttributeSelection);

            result.Store = ctx.Stores.ContainsKey(cartItem.StoreId)
                ? DataExporter.ToDynamic(ctx.Stores[cartItem.StoreId])
                : null;

            result.Customer = ToDynamic(cartItem.Customer);
            result.Product = await ToDynamic(cartItem.Product, ctx);

            return result;
        }

        private static dynamic CreateDynamic<TEntity>(TEntity entity)
            where TEntity : BaseEntity
        {
            if (entity == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(entity);
            return result;
        }

        private static List<dynamic> GetLocalized<TEntity>(
            DataExporterContext ctx,
            TEntity entity,
            params Expression<Func<TEntity, string>>[] keySelectors)
            where TEntity : BaseEntity, ILocalizedEntity
        {
            if (ctx.Languages.Count <= 1)
            {
                return null;
            }

            var translations = ctx.GetTranslations<TEntity>();
            if (translations == null)
            {
                return null;
            }

            var slugs = ctx.GetUrlRecords<TEntity>();
            var result = new List<dynamic>();

            foreach (var language in ctx.Languages)
            {
                var languageCulture = language.Value.LanguageCulture.EmptyNull().ToLower();

                // Add slug.
                if (slugs != null)
                {
                    var value = slugs.GetSlug(language.Value.Id, entity.Id, false);
                    if (value.HasValue())
                    {
                        dynamic exp = new HybridExpando();
                        exp.Culture = languageCulture;
                        exp.LocaleKey = "SeName";
                        exp.LocaleValue = value;

                        result.Add(exp);
                    }
                }

                // Add localized property value.
                foreach (var keySelector in keySelectors)
                {
                    var member = keySelector.Body as MemberExpression;
                    var propInfo = member.Member as PropertyInfo;
                    string localeKey = propInfo.Name;
                    var value = translations.GetValue(language.Value.Id, entity.Id, localeKey);

                    // We do not export empty values to not fill databases with it.
                    if (value.HasValue())
                    {
                        dynamic exp = new HybridExpando();
                        exp.Culture = languageCulture;
                        exp.LocaleKey = localeKey;
                        exp.LocaleValue = value;

                        result.Add(exp);
                    }
                }
            }

            return result.Any() ? result : null;
        }
    }
}
