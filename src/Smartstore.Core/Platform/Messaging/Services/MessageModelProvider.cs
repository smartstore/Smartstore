using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging.Events;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Imaging;
using Smartstore.Templating;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.Messaging
{
    public enum ModelTreeMemberKind
    {
        Primitive,
        Complex,
        Collection,
        Root
    }

    public class ModelTreeMember
    {
        public string Name { get; set; }
        public ModelTreeMemberKind Kind { get; set; }
    }

    // TODO: (mh) (core) Move poll model creation to external modules when they are available (via MessageModelPartMappingEvent)

    public partial class MessageModelProvider : IMessageModelProvider
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ITemplateEngine _templateEngine;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ILocalizationService _localizationService;
        private readonly ModuleManager _moduleManager;
        private readonly IUrlHelper _urlHelper;
        
        public MessageModelProvider(
            SmartDbContext db,
            ICommonServices services,
            ITemplateEngine templateEngine,
            IEmailAccountService emailAccountService,
            ILocalizationService localizationService,
            ModuleManager moduleManager,
            IUrlHelper urlHelper)
        {
            _db = db;
            _services = services;
            _templateEngine = templateEngine;
            _emailAccountService = emailAccountService;
            _localizationService = localizationService;
            _moduleManager = moduleManager;
            _urlHelper = urlHelper;
        }

        public LocalizerEx T { get; set; } = NullLocalizer.InstanceEx;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual async Task AddGlobalModelPartsAsync(MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));

            var model = messageContext.Model;

            model["Context"] = new Dictionary<string, object>
            {
                { "TemplateName", messageContext.MessageTemplate.Name },
                { "LanguageId", messageContext.Language.Id },
                { "LanguageCulture", messageContext.Language.LanguageCulture },
                { "LanguageRtl", messageContext.Language.Rtl },
                { "BaseUrl", messageContext.BaseUri.ToString() }
            };

            dynamic email = new ExpandoObject();
            email.Email = messageContext.EmailAccount.Email;
            email.SenderName = messageContext.EmailAccount.DisplayName;
            email.DisplayName = messageContext.EmailAccount.DisplayName; // Alias
            model["Email"] = email;
            model["Theme"] = CreateThemeModelPart(messageContext);
            model["Customer"] = await CreateModelPartAsync(messageContext.Customer, messageContext);
            model["Store"] = await CreateModelPartAsync(messageContext.Store, messageContext);
        }

        public async Task<object> CreateModelPartAsync(object part, bool ignoreNullMembers, params string[] ignoreMemberNames)
        {
            Guard.NotNull(part, nameof(part));

            var store = _services.StoreContext.CurrentStore;
            var messageContext = new MessageContext
            {
                Language = _services.WorkContext.WorkingLanguage,
                Store = store,
                BaseUri = new Uri(store.GetHost()),
                Model = new TemplateModel()
            };

            if (part is Customer x)
            {
                // This case is not handled in AddModelPart core method.
                messageContext.Customer = x;
                messageContext.Model["Part"] = await CreateModelPartAsync(x, messageContext);
            }
            else
            {
                messageContext.Customer = _services.WorkContext.CurrentCustomer;
                await AddModelPartAsync(part, messageContext, "Part");
            }

            object result = null;

            if (messageContext.Model.Any())
            {
                result = messageContext.Model.FirstOrDefault().Value;

                if (result is IDictionary<string, object> dict)
                {
                    SanitizeModelDictionary(dict, ignoreNullMembers, ignoreMemberNames);
                }
            }

            return result;
        }

        private void SanitizeModelDictionary(IDictionary<string, object> dict, bool ignoreNullMembers, params string[] ignoreMemberNames)
        {
            if (ignoreNullMembers || ignoreMemberNames.Length > 0)
            {
                foreach (var key in dict.Keys.ToArray())
                {
                    var expando = dict as HybridExpando;
                    var value = dict[key];

                    if ((ignoreNullMembers && value == null) || ignoreMemberNames.Contains(key))
                    {
                        if (expando != null)
                            expando.Override(key, null); // INFO: we cannot remove entries from HybridExpando
                        else
                            dict.Remove(key);
                        continue;
                    }

                    if (value != null && value.GetType().IsSequenceType())
                    {
                        var ignoreMemberNames2 = ignoreMemberNames
                            .Where(x => x.StartsWith(key + ".", StringComparison.OrdinalIgnoreCase))
                            .Select(x => x[(key.Length + 1)..])
                            .ToArray();

                        if (value is IDictionary<string, object> dict2)
                        {
                            SanitizeModelDictionary(dict2, ignoreNullMembers, ignoreMemberNames2);
                        }
                        else
                        {
                            var list = ((IEnumerable)value).OfType<IDictionary<string, object>>();
                            foreach (var dict3 in list)
                            {
                                SanitizeModelDictionary(dict3, ignoreNullMembers, ignoreMemberNames2);
                            }
                        }
                    }
                }
            }
        }

        public virtual async Task AddModelPartAsync(object part, MessageContext messageContext, string name = null)
        {
            Guard.NotNull(part, nameof(part));
            Guard.NotNull(messageContext, nameof(messageContext));

            var model = messageContext.Model;

            name = name.NullEmpty() ?? ResolveModelName(part);

            object modelPart = null;

            // TODO: (mh) (core) Uncomment when available.
            switch (part)
            {
                case INamedModelPart x:
                    modelPart = x;
                    break;
                case IModelPart x:
                    MergeModelBag(x, model, messageContext);
                    break;
                case Order x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case Product x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case Customer x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case Address x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case Shipment x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case OrderNote x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case RecurringPayment x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case ReturnRequest x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case GiftCard x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case NewsletterSubscription x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case Campaign x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case ProductReview x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case IEnumerable<GenericAttribute> x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                //case PollVotingRecord x:
                //    modelPart = await CreateModelPartAsync(x, messageContext);
                //    break;
                case ProductReviewHelpfulness x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                case BackInStockSubscription x:
                    modelPart = await CreateModelPartAsync(x, messageContext);
                    break;
                default:
                    var partType = part.GetType();
                    modelPart = part;

                    if (partType.IsPlainObjectType() && !partType.IsAnonymous())
                    {
                        var evt = new MessageModelPartMappingEvent(part, messageContext);
                        await _services.EventPublisher.PublishAsync(evt);

                        if (evt.Result != null && !object.ReferenceEquals(evt.Result, part))
                        {
                            _ = evt.Result;
                            name = evt.ModelPartName.NullEmpty() ?? ResolveModelName(evt.Result) ?? name;
                        }

                        modelPart = evt.Result ?? part;
                        name = evt.ModelPartName.NullEmpty() ?? name;
                    }

                    break;
            }

            if (modelPart != null)
            {
                if (name.IsEmpty())
                {
                    throw new SmartException($"Could not resolve a model key for part '{modelPart.GetType().Name}'. Use an instance of 'NamedModelPart' class to pass model with name.");
                }

                if (model.TryGetValue(name, out var existing))
                {
                    // A model part with the same name exists in model already...
                    if (existing is IDictionary<string, object> x)
                    {
                        // but it's a dictionary which we can easily merge with.
                        x.Merge(FastProperty.ObjectToDictionary(modelPart), true);
                    }
                    else
                    {
                        // Wrap in HybridExpando and merge.
                        var he = new HybridExpando(existing, true);
                        he.Merge(FastProperty.ObjectToDictionary(modelPart), true);
                        model[name] = he;
                    }
                }
                else
                {
                    // Put part to model as new property.
                    model[name] = modelPart;

                    if (name == nameof(NewsletterSubscription))
                    {
                        // Info: Legacy code to support old message template tokens. 
                        // TODO: (mh) (core) Write migration to translate this token in the database. Then change it in all email templates.
                        model["NewsLetterSubscription"] = modelPart;
                    }
                }
            }
        }

        public string ResolveModelName(object model)
        {
            Guard.NotNull(model, nameof(model));

            string name = null;
            var type = model.GetType();

            try
            {
                if (model is INamedEntity be)
                {
                    name = be.GetEntityName();
                }
                else if (model is ITestModel te)
                {
                    name = te.ModelName;
                }
                else if (model is INamedModelPart mp)
                {
                    name = mp.ModelPartName;
                }
                else if (type.IsPlainObjectType())
                {
                    name = type.Name;
                }
            }
            catch 
            { 
            }

            return name;
        }

        #region Global model part handlers

        protected virtual object CreateThemeModelPart(MessageContext messageContext)
        {
            var m = new Dictionary<string, object>
            {
                { "FontFamily", "-apple-system, system-ui, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif" },
                { "BodyBg", "#f2f4f6" },
                { "BodyColor", "#555" },
                { "TitleColor", "#2f3133" },
                { "ContentBg", "#fff" },
                { "ShadeColor", "#e2e2e2" },
                { "LinkColor", "#0066c0" },
                { "BrandPrimary", "#3f51b5" },
                { "BrandSuccess", "#4caf50" },
                { "BrandWarning", "#ff9800" },
                { "BrandDanger", "#f44336" },
                { "MutedColor", "#a5a5a5" },
            };

            return m;
        }

        protected virtual async Task<object> CreateCompanyModelPartAsync(MessageContext messageContext)
        {
            var settings = await _services.SettingFactory.LoadSettingsAsync<CompanyInformationSettings>(messageContext.Store.Id);
            var country = await _db.Countries.FindByIdAsync(settings.CountryId, false);

            dynamic m = new HybridExpando(settings, true);

            m.NameLine = Concat(settings.Salutation, settings.Title, settings.Firstname, settings.Lastname);
            m.StreetLine = Concat(settings.Street, settings.Street2);
            m.CityLine = Concat(settings.ZipCode, settings.City);

            if (country != null)
            {
                m.CountryLine = Concat(country.GetLocalized(x => x.Name), settings.Region);
            }

            await PublishModelPartCreatedEventAsync<CompanyInformationSettings>(settings, m);
            return m;
        }

        protected virtual async Task<object> CreateBankModelPartAsync(MessageContext messageContext)
        {
            var settings = await _services.SettingFactory.LoadSettingsAsync<BankConnectionSettings>(messageContext.Store.Id);
            var m = new HybridExpando(settings, true);
            await PublishModelPartCreatedEventAsync(settings, m);
            return m;
        }

        protected virtual async Task<object> CreateContactModelPartAsync(MessageContext messageContext)
        {
            var settings = await _services.SettingFactory.LoadSettingsAsync<ContactDataSettings>(messageContext.Store.Id);
            var contact = new HybridExpando(settings, true) as dynamic;

            // Aliases
            contact.Phone = new
            {
                Company = settings.CompanyTelephoneNumber.NullEmpty(),
                Hotline = settings.HotlineTelephoneNumber.NullEmpty(),
                Mobile = settings.MobileTelephoneNumber.NullEmpty(),
                Fax = settings.CompanyFaxNumber.NullEmpty()
            };

            contact.Email = new
            {
                Company = settings.CompanyEmailAddress.NullEmpty(),
                Webmaster = settings.WebmasterEmailAddress.NullEmpty(),
                Support = settings.SupportEmailAddress.NullEmpty(),
                Contact = settings.ContactEmailAddress.NullEmpty()
            };

            await PublishModelPartCreatedEventAsync<ContactDataSettings>(settings, contact);

            return contact;
        }

        #endregion

        #region Generic model part handlers

        protected virtual void MergeModelBag(IModelPart part, IDictionary<string, object> model, MessageContext messageContext)
        {
            if (model.Get("Bag") is not IDictionary<string, object> bag)
            {
                model["Bag"] = bag = new Dictionary<string, object>();
            }

            var source = part as IDictionary<string, object>;
            bag.Merge(source);
        }

        #endregion

        #region Entity specific model part handlers

        protected virtual async Task<object> CreateModelPartAsync(Store part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var host = messageContext.BaseUri.ToString();
            var logoFile = await _services.MediaService.GetFileByIdAsync(messageContext.Store.LogoMediaFileId, MediaLoadFlags.AsNoTracking);
            
            var m = new Dictionary<string, object>
            {
                { "Email", messageContext.EmailAccount.Email },
                { "EmailName", messageContext.EmailAccount.DisplayName },
                { "Name", part.Name },
                { "Url", host },
                { "Cdn", part.ContentDeliveryNetwork },
                { "PrimaryStoreCurrency", part.PrimaryStoreCurrency?.CurrencyCode },
                { "PrimaryExchangeRateCurrency", part.PrimaryExchangeRateCurrency?.CurrencyCode },
                { "Logo", await CreateModelPartAsync(logoFile, messageContext, host, null, new Size(400, 75)) },
                { "Company", await CreateCompanyModelPartAsync(messageContext) },
                { "Contact", await CreateContactModelPartAsync(messageContext) },
                { "Bank", await CreateBankModelPartAsync(messageContext) },
                { "Copyright", T("Content.CopyrightNotice", messageContext.Language.Id, DateTime.Now.Year.ToString(), part.Name).ToString() }
            };

            var he = new HybridExpando(true);
            he.Merge(m, true);

            await PublishModelPartCreatedEventAsync(part, he);

            return he;
        }

        protected virtual async Task<object> CreateModelPartAsync(
            MediaFileInfo part,
            MessageContext messageContext,
            string href,
            int? targetSize = null,
            Size? clientMaxSize = null,
            string alt = null)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotEmpty(href, nameof(href));

            if (part == null)
            {
                return null;
            }

            var width = part.File.Width;
            var height = part.File.Height;

            if (width.HasValue && height.HasValue && (targetSize.HasValue || clientMaxSize.HasValue))
            {
                var maxSize = clientMaxSize ?? new Size(targetSize.Value, targetSize.Value);
                var size = ImagingHelper.Rescale(new Size(width.Value, height.Value), maxSize);
                width = size.Width;
                height = size.Height;
            }

            var m = new
            {
                Src = _services.MediaService.GetUrl(part, targetSize.GetValueOrDefault(), messageContext.BaseUri.ToString(), false),
                Href = href,
                Width = width,
                Height = height,
                Alt = alt
            };

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(Product part, MessageContext messageContext, ProductVariantAttributeSelection attrSelection = null)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var mediaSettings = await _services.SettingFactory.LoadSettingsAsync<MediaSettings>((int)messageContext.StoreId);
            var shoppingCartSettings = await _services.SettingFactory.LoadSettingsAsync<ShoppingCartSettings>((int)messageContext.StoreId);
            var catalogSettings = await _services.SettingFactory.LoadSettingsAsync<CatalogSettings>((int)messageContext.StoreId);

            var currencyService = _services.Resolve<ICurrencyService>();
            var productUrlHelper = _services.Resolve<ProductUrlHelper>();

            var quantityUnit = await _db.QuantityUnits.FindByIdAsync(part.QuantityUnitId ?? 0, false);
            var deliveryTime = await _db.DeliveryTimes.FindByIdAsync(part.DeliveryTimeId ?? 0, false);
            var additionalShippingCharge = currencyService.ConvertToWorkingCurrency(part.AdditionalShippingCharge).WithSymbol(false);

            var slug = await part.GetActiveSlugAsync(messageContext.Language.Id);
            var productUrl = await productUrlHelper.GetProductUrlAsync(part.Id, slug, attrSelection);
            var url = BuildUrl(productUrl, messageContext);
            var file = await GetMediaFileFor(part, attrSelection);
            var name = part.GetLocalized(x => x.Name, messageContext.Language.Id).Value;
            var alt = T("Media.Product.ImageAlternateTextFormat", messageContext.Language.Id, name).ToString();

            var m = new Dictionary<string, object>
            {
                { "Id", part.Id },
                { "Sku", catalogSettings.ShowProductSku ? part.Sku : null },
                { "Name", name },
                { "Description", part.GetLocalized(x => x.ShortDescription, messageContext.Language).Value.NullEmpty() },
                { "StockQuantity", part.StockQuantity },
                { "AdditionalShippingCharge", additionalShippingCharge },
                { "Url", url },
                { "Thumbnail", await CreateModelPartAsync(file, messageContext, url, mediaSettings.MessageProductThumbPictureSize, new Size(50, 50), alt) },
                { "ThumbnailLg", await CreateModelPartAsync (file, messageContext, url, mediaSettings.ProductThumbPictureSize, new Size(120, 120), alt) },
                { "DeliveryTime", null },
                { "QtyUnit", null }
            };

            if (part.IsShippingEnabled && shoppingCartSettings.DeliveryTimesInShoppingCart != DeliveryTimesPresentation.None)
            {
                if (deliveryTime is DeliveryTime dt)
                {
                    m["DeliveryTime"] = new Dictionary<string, object>
                    {
                        { "Color", dt.ColorHexValue },
                        { "Name", dt.GetLocalized(x => x.Name, messageContext.Language).Value },
                    };
                }
            }

            if (quantityUnit is QuantityUnit qu)
            {
                m["QtyUnit"] = qu.GetLocalized(x => x.Name, messageContext.Language).Value;
            }

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(Customer part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var email = part.FindEmail();
            var pwdRecoveryToken = part.GenericAttributes.PasswordRecoveryToken;
            var accountActivationToken = part.GenericAttributes.AccountActivationToken;
            var customerVatStatus = (VatNumberStatus)part.VatNumberStatusId;

            // TODO: (mh) (core) > Uncomment when available
            //int rewardPointsBalance = part.GetRewardPointsBalance();
            //decimal rewardPointsAmountBase = _services.Resolve<IOrderTotalCalculationService>().ConvertRewardPointsToAmount(rewardPointsBalance);
            //decimal rewardPointsAmount = _services.Resolve<ICurrencyService>().ConvertFromPrimaryStoreCurrency(rewardPointsAmountBase, _services.WorkContext.WorkingCurrency);

            var m = new Dictionary<string, object>
            {
                ["Id"] = part.Id,
                ["CustomerGuid"] = part.CustomerGuid,
                ["Username"] = part.Username,
                ["Email"] = email,
                ["IsTaxExempt"] = part.IsTaxExempt,
                ["LastIpAddress"] = part.LastIpAddress,
                ["CreatedOn"] = ToUserDate(part.CreatedOnUtc, messageContext),
                ["LastLoginOn"] = ToUserDate(part.LastLoginDateUtc, messageContext),
                ["LastActivityOn"] = ToUserDate(part.LastActivityDateUtc, messageContext),
                ["FullName"] = GetDisplayNameForCustomer(part).NullEmpty(),
                ["VatNumber"] = part.GenericAttributes.VatNumber,
                ["VatNumberStatus"] = customerVatStatus.GetLocalizedEnum(messageContext.Language.Id).NullEmpty(),
                ["CustomerNumber"] = part.CustomerNumber.NullEmpty(),
                ["IsRegistered"] = part.IsRegistered(),

                // URLs
                ["WishlistUrl"] = BuildRouteUrl("Wishlist", new { customerGuid = part.CustomerGuid }, messageContext),
                ["EditUrl"] = BuildActionUrl("Edit", "Customer", new { id = part.Id, area = "Admin" }, messageContext),
                ["PasswordRecoveryURL"] = pwdRecoveryToken == null ? null : BuildActionUrl("passwordrecoveryconfirm", "customer",
                    new { token = pwdRecoveryToken, email, area = "" }, 
                    messageContext),
                ["AccountActivationURL"] = accountActivationToken == null ? null : BuildActionUrl("activation", "customer", 
                    new { token = accountActivationToken, email, area = "" }, 
                    messageContext),

                // Addresses
                ["BillingAddress"] = await CreateModelPartAsync(part.BillingAddress ?? new Address(), messageContext),
                ["ShippingAddress"] = part.ShippingAddress == null ? null : await CreateModelPartAsync(part.ShippingAddress, messageContext),

                // TODO: (mh) (core) > Uncomment when available
                // Reward Points
                //["RewardPointsAmount"] = rewardPointsAmount,
                //["RewardPointsBalance"] = FormatPrice(rewardPointsAmount, messageContext),
                //["RewardPointsHistory"] = part.RewardPointsHistory.Count == 0 ? null : part.RewardPointsHistory.Select(x => CreateModelPart(x, messageContext)).ToList(),
            };

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(GiftCard part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                { "Id", part.Id },
                { "SenderName", part.SenderName.NullEmpty() },
                { "SenderEmail", part.SenderEmail.NullEmpty() },
                { "RecipientName", part.RecipientName.NullEmpty() },
                { "RecipientEmail", part.RecipientEmail.NullEmpty() },
                { "Amount", FormatPrice(part.Amount, messageContext) },
                { "CouponCode", part.GiftCardCouponCode.NullEmpty() }
            };

            // Message
            var message = (string)null;
            if (part.Message.HasValue())
            {
                message = HtmlUtils.StripTags(part.Message);
            }
            m["Message"] = message;

            // RemainingAmount
            Money remainingAmount = new();
            var order = part?.PurchasedWithOrderItem?.Order;
            if (order != null)
            {
                var remainingAmountBase = _services.Resolve<IGiftCardService>().GetRemainingAmount(part);
                remainingAmount = FormatPrice(remainingAmountBase.Amount, order, messageContext);
            }
            m["RemainingAmount"] = remainingAmount;

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(NewsletterSubscription part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var gid = part.NewsletterSubscriptionGuid;

            var m = new Dictionary<string, object>
            {
                { "Id", part.Id },
                { "Email", part.Email.NullEmpty() },
                { "ActivationUrl", gid == Guid.Empty ? null : BuildRouteUrl("NewsletterActivation", new { token = part.NewsletterSubscriptionGuid, active = true }, messageContext) },
                { "DeactivationUrl", gid == Guid.Empty ? null : BuildRouteUrl("NewsletterActivation", new { token = part.NewsletterSubscriptionGuid, active = false }, messageContext) }
            };

            var customer = messageContext.Customer;
            if (customer != null && customer.Email.EqualsNoCase(part.Email.EmptyNull()))
            {
                // Set FullName only if a customer account exists for the subscriber's email address.
                m["FullName"] = customer.GetFullName();
            }

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(Campaign part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var protocol = messageContext.BaseUri.Scheme;
            var host = messageContext.BaseUri.Authority + messageContext.BaseUri.AbsolutePath;
            var body = HtmlUtils.RelativizeFontSizes(part.Body.EmptyNull());

            // We must render the body separately.
            body = await _templateEngine.RenderAsync(body, messageContext.Model, messageContext.FormatProvider);

            var m = new Dictionary<string, object>
            {
                { "Id", part.Id },
                { "Subject", part.Subject.NullEmpty() },
                { "Body", WebHelper.MakeAllUrlsAbsolute(body, protocol, host).NullEmpty() },
                { "CreatedOn", ToUserDate(part.CreatedOnUtc, messageContext) }
            };

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(ProductReview part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                { "Title", part.Title.NullEmpty() },
                { "Text", HtmlUtils.StripTags(part.ReviewText).NullEmpty() },
                { "Rating", part.Rating }
            };

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(ProductReviewHelpfulness part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                { "ProductReviewId", part.ProductReviewId },
                { "ReviewTitle", part.ProductReview.Title },
                { "WasHelpful", part.WasHelpful }
            };

            ApplyCustomerContentPart(m, part, messageContext);

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        //protected virtual object CreateModelPart(PollVotingRecord part, MessageContext messageContext)
        //{
        //    Guard.NotNull(messageContext, nameof(messageContext));
        //    Guard.NotNull(part, nameof(part));

        //    var m = new Dictionary<string, object>
        //    {
        //        { "PollAnswerId", part.PollAnswerId },
        //        { "PollAnswerName", part.PollAnswer.Name },
        //        { "PollId", part.PollAnswer.PollId }
        //    };

        //    ApplyCustomerContentPart(m, part, messageContext);

        //    PublishModelPartCreatedEvent<PollVotingRecord>(part, m);

        //    return m;
        //}

        protected virtual async Task<object> CreateModelPartAsync(Address part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var settings = await _services.SettingFactory.LoadSettingsAsync<AddressSettings>();
            var languageId = messageContext.Language?.Id ?? messageContext.LanguageId;

            var salutation = part.Salutation.NullEmpty();
            var title = part.Title.NullEmpty();
            var fullSalutation = $"{salutation}{(title.HasValue() ? " " + title : string.Empty)}".NullEmpty();
            var company = settings.CompanyEnabled ? part.Company : null;
            var firstName = part.FirstName.NullEmpty();
            var lastName = part.LastName.NullEmpty();
            var street1 = settings.StreetAddressEnabled ? part.Address1 : null;
            var street2 = settings.StreetAddress2Enabled ? part.Address2 : null;
            var zip = settings.ZipPostalCodeEnabled ? part.ZipPostalCode : null;
            var city = settings.CityEnabled ? part.City : null;
            var country = settings.CountryEnabled ? part.Country?.GetLocalized(x => x.Name, languageId ?? 0)?.Value.NullEmpty() : null;
            var state = settings.StateProvinceEnabled ? part.StateProvince?.GetLocalized(x => x.Name, languageId ?? 0)?.Value.NullEmpty() : null;

            var m = new Dictionary<string, object>
            {
                { "Title", title },
                { "Salutation", salutation },
                { "FullSalutation", fullSalutation },
                { "FullName", part.GetFullName(false).NullEmpty() },
                { "Company", company },
                { "FirstName", firstName },
                { "LastName", lastName },
                { "Street1", street1 },
                { "Street2", street2 },
                { "Country", country },
                { "CountryId", part.Country?.Id },
                { "CountryAbbrev2", settings.CountryEnabled ? part.Country?.TwoLetterIsoCode.NullEmpty() : null },
                { "CountryAbbrev3", settings.CountryEnabled ? part.Country?.ThreeLetterIsoCode.NullEmpty() : null },
                { "State", state },
                { "StateAbbrev", settings.StateProvinceEnabled ? part.StateProvince?.Abbreviation.NullEmpty() : null },
                { "City", city },
                { "ZipCode", zip },
                { "Email", part.Email.NullEmpty() },
                { "Phone", settings.PhoneEnabled ? part.PhoneNumber : null },
                { "Fax", settings.FaxEnabled ? part.FaxNumber : null }
            };

            m["NameLine"] = Concat(salutation, title, firstName, lastName);
            m["StreetLine"] = Concat(street1, street2);
            m["CityLine"] = Concat(zip, city);
            m["CountryLine"] = Concat(country, state);

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(RewardPointsHistory part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                { "Id", part.Id },
                { "CreatedOn", ToUserDate(part.CreatedOnUtc, messageContext) },
                { "Message", part.Message.NullEmpty() },
                { "Points", part.Points },
                { "PointsBalance", part.PointsBalance },
                { "UsedAmount", part.UsedAmount }
            };

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(IEnumerable<GenericAttribute> part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>();

            foreach (var attr in part)
            {
                m[attr.Key] = attr.Value;
            }

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(BackInStockSubscription part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                {  "StoreId", part.StoreId },
                {  "CustomerId",  part.CustomerId },
                {  "ProductId",  part.ProductId },
                {  "CreatedOn", ToUserDate(part.CreatedOnUtc, messageContext) }
            };

            await PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        #endregion

        #region Model Tree

        public async Task<TreeNode<ModelTreeMember>> GetLastModelTreeAsync(string messageTemplateName)
        {
            Guard.NotEmpty(messageTemplateName, nameof(messageTemplateName));

            var template = await _db.MessageTemplates
                .AsNoTracking()
                .Where(x => x.Name == messageTemplateName)
                .ApplyStoreFilter(_services.StoreContext.CurrentStore.Id)
                .FirstOrDefaultAsync();

            if (template != null)
            {
                return GetLastModelTree(template);
            }

            return null;
        }

        public TreeNode<ModelTreeMember> GetLastModelTree(MessageTemplate template)
        {
            Guard.NotNull(template, nameof(template));

            if (template.LastModelTree.IsEmpty())
            {
                return null;
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<TreeNode<ModelTreeMember>>(template.LastModelTree);
        }

        public TreeNode<ModelTreeMember> BuildModelTree(TemplateModel model)
        {
            Guard.NotNull(model, nameof(model));

            var root = new TreeNode<ModelTreeMember>(new ModelTreeMember { Name = "Model", Kind = ModelTreeMemberKind.Root });

            foreach (var kvp in model)
            {
                root.Append(BuildModelTreePart(kvp.Key, kvp.Value));
            }

            return root;
        }

        private TreeNode<ModelTreeMember> BuildModelTreePart(string modelName, object instance)
        {
            var t = instance?.GetType();
            TreeNode<ModelTreeMember> node;
            if (t == null || t.IsPredefinedType())
            {
                node = new TreeNode<ModelTreeMember>(new ModelTreeMember { Name = modelName, Kind = ModelTreeMemberKind.Primitive });
            }
            else if (t.IsSequenceType() && !(instance is IDictionary<string, object>))
            {
                node = new TreeNode<ModelTreeMember>(new ModelTreeMember { Name = modelName, Kind = ModelTreeMemberKind.Collection });
            }
            else
            {
                node = new TreeNode<ModelTreeMember>(new ModelTreeMember { Name = modelName, Kind = ModelTreeMemberKind.Complex });

                if (instance is IDictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        node.Append(BuildModelTreePart(kvp.Key, kvp.Value));
                    }
                }
                else if (instance is IDynamicMetaObjectProvider dyn)
                {
                    foreach (var name in dyn.GetMetaObject(Expression.Constant(dyn)).GetDynamicMemberNames())
                    {
                        // we don't want to go deeper in "pure" dynamic objects
                        node.Append(new TreeNode<ModelTreeMember>(new ModelTreeMember { Name = name, Kind = ModelTreeMemberKind.Primitive }));
                    }
                }
                else
                {
                    node.AppendRange(BuildModelTreePartForClass(instance));
                }
            }

            return node;
        }

        private IEnumerable<TreeNode<ModelTreeMember>> BuildModelTreePartForClass(object instance)
        {
            var type = instance?.GetType();

            if (type == null)
            {
                yield break;
            }

            foreach (var prop in FastProperty.GetProperties(type).Values)
            {
                var pi = prop.Property;

                if (pi.PropertyType.IsPredefinedType())
                {
                    yield return new TreeNode<ModelTreeMember>(new ModelTreeMember { Name = prop.Name, Kind = ModelTreeMemberKind.Primitive });
                }
                else if (typeof(IDictionary<string, object>).IsAssignableFrom(pi.PropertyType))
                {
                    yield return BuildModelTreePart(prop.Name, prop.GetValue(instance));
                }
                else if (pi.PropertyType.IsSequenceType())
                {
                    yield return new TreeNode<ModelTreeMember>(new ModelTreeMember { Name = prop.Name, Kind = ModelTreeMemberKind.Collection });
                }
                else
                {
                    var node = new TreeNode<ModelTreeMember>(new ModelTreeMember { Name = prop.Name, Kind = ModelTreeMemberKind.Complex });
                    node.AppendRange(BuildModelTreePartForClass(prop.GetValue(instance)));
                    yield return node;
                }
            }
        }

        #endregion
    }
}
