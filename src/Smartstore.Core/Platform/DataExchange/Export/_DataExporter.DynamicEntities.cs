using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media;
using Smartstore.Core.DataExchange.Export.Internal;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExporter
    {
        private readonly string[] _orderCustomerAttributes = new[]
        {
            SystemCustomerAttributeNames.VatNumber,
            SystemCustomerAttributeNames.ImpersonatedCustomerId
        };

        private static dynamic ToDynamic<T>(T entity)
        {
            if (entity == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(entity);
            return result;
        }

        private static dynamic ToDynamic(Currency currency, DataExporterContext ctx)
        {
            if (currency == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(currency);
            var translations = ctx.Translations[nameof(Currency)];

            result.Name = translations.GetValue(ctx.LanguageId, currency.Id, nameof(currency.Name)) ?? currency.Name;
            result._Localized = GetLocalized(ctx, translations, null, currency, x => x.Name);

            return result;
        }

        private static dynamic ToDynamic(Country country, DataExporterContext ctx)
        {
            if (country == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(country);
            var translations = ctx.Translations[nameof(Country)];

            result.Name = translations.GetValue(ctx.LanguageId, country.Id, nameof(country.Name)) ?? country.Name;
            result._Localized = GetLocalized(ctx, translations, null, country, x => x.Name);

            return result;
        }

        private static dynamic ToDynamic(Address address, DataExporterContext ctx)
        {
            if (address == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(address);

            result.Country = ToDynamic(address.Country, ctx);

            if (address.StateProvinceId.GetValueOrDefault() > 0)
            {
                dynamic sp = new DynamicEntity(address.StateProvince);
                var translations = ctx.Translations[nameof(StateProvince)];

                sp.Name = translations.GetValue(ctx.LanguageId, address.StateProvince.Id, nameof(StateProvince)) ?? address.StateProvince.Name;
                sp._Localized = GetLocalized(ctx, translations, null, address.StateProvince, x => x.Name);

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

        private static dynamic ToDynamic(Store store, DataExporterContext ctx)
        {
            if (store == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(store);

            result.PrimaryStoreCurrency = ToDynamic(store.PrimaryStoreCurrency, ctx);
            result.PrimaryExchangeRateCurrency = ToDynamic(store.PrimaryExchangeRateCurrency, ctx);

            return result;
        }

        private static dynamic ToDynamic(DeliveryTime deliveryTime, DataExporterContext ctx)
        {
            if (deliveryTime == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(deliveryTime);
            var translations = ctx.Translations[nameof(DeliveryTime)];

            result.Name = translations.GetValue(ctx.LanguageId, deliveryTime.Id, nameof(deliveryTime.Name)) ?? deliveryTime.Name;
            result._Localized = GetLocalized(ctx, translations, null, deliveryTime, x => x.Name);

            return result;
        }

        private static dynamic ToDynamic(QuantityUnit quantityUnit, DataExporterContext ctx)
        {
            if (quantityUnit == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(quantityUnit);
            var translations = ctx.Translations[nameof(QuantityUnit)];

            result.Name = translations.GetValue(ctx.LanguageId, quantityUnit.Id, nameof(quantityUnit.Name)) ?? quantityUnit.Name;
            result.NamePlural = translations.GetValue(ctx.LanguageId, quantityUnit.Id, nameof(quantityUnit.NamePlural)) ?? quantityUnit.NamePlural;
            result.Description = translations.GetValue(ctx.LanguageId, quantityUnit.Id, nameof(quantityUnit.Description)) ?? quantityUnit.Description;

            result._Localized = GetLocalized(ctx, translations, null, quantityUnit,
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
            var translations = ctx.Translations[nameof(Manufacturer)];
            var urlRecords = ctx.UrlRecords[nameof(Manufacturer)];

            result.Picture = null;
            result.Name = translations.GetValue(ctx.LanguageId, manufacturer.Id, nameof(manufacturer.Name)) ?? manufacturer.Name;

            if (!ctx.IsPreview)
            {
                result.SeName = ctx.UrlRecords[nameof(Manufacturer)].GetSlug(ctx.LanguageId, manufacturer.Id);
                result.Description = translations.GetValue(ctx.LanguageId, manufacturer.Id, nameof(manufacturer.Description)) ?? manufacturer.Description;
                result.BottomDescription = translations.GetValue(ctx.LanguageId, manufacturer.Id, nameof(manufacturer.BottomDescription)) ?? manufacturer.BottomDescription;
                result.MetaKeywords = translations.GetValue(ctx.LanguageId, manufacturer.Id, nameof(manufacturer.MetaKeywords)) ?? manufacturer.MetaKeywords;
                result.MetaDescription = translations.GetValue(ctx.LanguageId, manufacturer.Id, nameof(manufacturer.MetaDescription)) ?? manufacturer.MetaDescription;
                result.MetaTitle = translations.GetValue(ctx.LanguageId, manufacturer.Id, nameof(manufacturer.MetaTitle)) ?? manufacturer.MetaTitle;

                result._Localized = GetLocalized(ctx, translations, urlRecords, manufacturer,
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
            var translations = ctx.Translations[nameof(Category)];
            var urlRecords = ctx.UrlRecords[nameof(Category)];

            result.Picture = null;
            result.Name = translations.GetValue(ctx.LanguageId, category.Id, nameof(category.Name)) ?? category.Name;
            result.FullName = translations.GetValue(ctx.LanguageId, category.Id, nameof(category.FullName)) ?? category.FullName;

            if (!ctx.IsPreview)
            {
                result.SeName = ctx.UrlRecords[nameof(Category)].GetSlug(ctx.LanguageId, category.Id);
                result.Description = translations.GetValue(ctx.LanguageId, category.Id, nameof(category.Description)) ?? category.Description;
                result.BottomDescription = translations.GetValue(ctx.LanguageId, category.Id, nameof(category.BottomDescription)) ?? category.BottomDescription;
                result.MetaKeywords = translations.GetValue(ctx.LanguageId, category.Id, nameof(category.MetaKeywords)) ?? category.MetaKeywords;
                result.MetaDescription = translations.GetValue(ctx.LanguageId, category.Id, nameof(category.MetaDescription)) ?? category.MetaDescription;
                result.MetaTitle = translations.GetValue(ctx.LanguageId, category.Id, nameof(category.MetaTitle)) ?? category.MetaTitle;

                result._CategoryTemplateViewPath = ctx.CategoryTemplates.ContainsKey(category.CategoryTemplateId)
                    ? ctx.CategoryTemplates[category.CategoryTemplateId]
                    : "";

                result._Localized = GetLocalized(ctx, translations, urlRecords, category,
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
            var paTranslations = ctx.TranslationsPerPage[nameof(ProductAttribute)];
            var pvavTranslations = ctx.TranslationsPerPage[nameof(ProductVariantAttributeValue)];

            dynAttribute.Name = paTranslations.GetValue(languageId, attribute.Id, nameof(attribute.Name)) ?? attribute.Name;
            dynAttribute.Description = paTranslations.GetValue(languageId, attribute.Id, nameof(attribute.Description)) ?? attribute.Description;

            dynAttribute.Values = productAttribute.ProductVariantAttributeValues
                .OrderBy(x => x.DisplayOrder)
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x);

                    dyn.Name = pvavTranslations.GetValue(languageId, x.Id, nameof(x.Name)) ?? x.Name;
                    dyn._Localized = GetLocalized(ctx, pvavTranslations, null, x, y => y.Name);

                    return dyn;
                })
                .ToList();

            dynAttribute._Localized = GetLocalized(ctx, paTranslations, null, attribute,
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
            var saTranslations = ctx.TranslationsPerPage[nameof(SpecificationAttribute)];
            var saoTranslations = ctx.TranslationsPerPage[nameof(SpecificationAttributeOption)];

            dynAttribute.Name = saTranslations.GetValue(ctx.LanguageId, attribute.Id, nameof(attribute.Name)) ?? attribute.Name;
            dynAttribute._Localized = GetLocalized(ctx, saTranslations, null, attribute, x => x.Name);

            dynAttribute.Alias = saTranslations.GetValue(ctx.LanguageId, attribute.Id, nameof(attribute.Alias)) ?? attribute.Alias;
            dynAttribute._Localized = GetLocalized(ctx, saTranslations, null, attribute, x => x.Alias);

            dynamic dynOption = new DynamicEntity(option);

            dynOption.Name = saoTranslations.GetValue(ctx.LanguageId, option.Id, nameof(option.Name)) ?? option.Name;
            dynOption._Localized = GetLocalized(ctx, saoTranslations, null, option, x => x.Name);

            dynOption.Alias = saoTranslations.GetValue(ctx.LanguageId, option.Id, nameof(option.Alias)) ?? option.Alias;
            dynOption._Localized = GetLocalized(ctx, saoTranslations, null, option, x => x.Alias);

            dynOption.SpecificationAttribute = dynAttribute;
            result.SpecificationAttributeOption = dynOption;

            return result;
        }

        private dynamic ToDynamic(MediaFile file, int thumbPictureSize, int detailsPictureSize, DataExporterContext ctx)
        {
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
                var host = ctx.Store.GetHost();
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

        private async Task<dynamic> ToDynamic(Order order, DataExporterContext ctx)
        {
            if (order == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(order);

            result.OrderNumber = order.GetOrderNumber();
            result.OrderStatus = await _services.Localization.GetLocalizedEnumAsync(order.OrderStatus, ctx.LanguageId);
            result.PaymentStatus = _services.Localization.GetLocalizedEnumAsync(order.PaymentStatus, ctx.LanguageId);
            result.ShippingStatus = _services.Localization.GetLocalizedEnumAsync(order.ShippingStatus, ctx.LanguageId);

            result.Customer = null;
            result.BillingAddress = null;
            result.ShippingAddress = null;
            result.Shipments = null;

            result.Store = ctx.Stores.ContainsKey(order.StoreId)
                ? ToDynamic(ctx.Stores[order.StoreId], ctx)
                : null;

            if (!ctx.IsPreview)
            {
                result.RedeemedRewardPointsEntry = ToDynamic(order.RedeemedRewardPointsEntry);
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
            result.Product = ToDynamic(orderItem.Product, ctx);

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
                ? ToDynamic(ctx.Stores[subscription.StoreId], ctx)
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
                ? ToDynamic(ctx.Stores[cartItem.StoreId], ctx)
                : null;

            result.Customer = ToDynamic(cartItem.Customer);
            result.Product = ToDynamic(cartItem.Product, ctx);

            return result;
        }

        private static List<dynamic> GetLocalized<T>(
            DataExporterContext ctx,
            LocalizedPropertyCollection translations,
            UrlRecordCollection urlRecords,
            T entity,
            params Expression<Func<T, string>>[] keySelectors)
            where T : BaseEntity, ILocalizedEntity
        {
            Guard.NotNull(translations, nameof(translations));

            if (ctx.Languages.Count <= 1)
            {
                return null;
            }

            var result = new List<dynamic>();

            foreach (var language in ctx.Languages)
            {
                var languageCulture = language.Value.LanguageCulture.EmptyNull().ToLower();

                // Add SEO name.
                if (urlRecords != null)
                {
                    var value = urlRecords.GetSlug(language.Value.Id, entity.Id, false);
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
