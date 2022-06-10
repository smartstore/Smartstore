using System.Globalization;
using System.Text;
using System.Xml;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Platform.DataExchange.Export
{
    /// <summary>
    /// Allows to exclude XML nodes from export.
    /// </summary>
    [Flags]
    public enum ExportXmlExclude
    {
        None = 0,
        Category = 1
    }

    /// <summary>
    /// Writes XML formatted entity data using <see cref="XmlWriter"/>.
    /// </summary>
    /// <remarks>
    /// Uses synchronous XmlWriter methods to avoid many atomic asynchronous write statements for each small text portion.
    /// https://stackoverflow.com/questions/16641074/xmlwriter-async-methods/37391267
    /// </remarks>
    public class ExportXmlHelper : Disposable
    {
        protected XmlWriter _writer;
        protected CultureInfo _culture;
        protected bool _doNotDispose;

        public ExportXmlHelper(XmlWriter writer, bool doNotDispose = false, CultureInfo culture = null)
        {
            _writer = writer;
            _doNotDispose = doNotDispose;
            _culture = culture ?? CultureInfo.InvariantCulture;
        }

        public ExportXmlHelper(Stream stream, XmlWriterSettings settings = null, CultureInfo culture = null)
        {
            _writer = XmlWriter.Create(stream, settings ?? DefaultSettings);
            _culture = culture ?? CultureInfo.InvariantCulture;
        }

        public static XmlWriterSettings DefaultSettings => new()
        {
            Encoding = Encoding.UTF8,
            CheckCharacters = false,
            Indent = false,
            NewLineHandling = NewLineHandling.None
        };

        public ExportXmlExclude Exclude { get; set; }

        public XmlWriter Writer => _writer;

        public void WriteLocalized(dynamic parentNode)
        {
            if (parentNode == null || parentNode._Localized == null)
                return;

            _writer.WriteStartElement("Localized");
            foreach (dynamic item in parentNode._Localized)
            {
                _writer.WriteStartElement((string)item.LocaleKey);
                _writer.WriteAttributeString("culture", (string)item.Culture);
                _writer.WriteString(((string)item.LocaleValue).RemoveInvalidXmlChars());
                _writer.WriteEndElement();
            }
            _writer.WriteEndElement();
        }

        public void WriteGenericAttributes(dynamic parentNode)
        {
            if (parentNode == null || parentNode._GenericAttributes == null)
                return;

            _writer.WriteStartElement("GenericAttributes");
            foreach (dynamic genericAttribute in parentNode._GenericAttributes)
            {
                GenericAttribute entity = genericAttribute.Entity;

                _writer.WriteStartElement("GenericAttribute");
                _writer.WriteElementString("Id", entity.Id.ToString());
                _writer.WriteElementString("EntityId", entity.EntityId.ToString());
                _writer.WriteElementString("KeyGroup", entity.KeyGroup);
                _writer.WriteElementString("Key", entity.Key);
                _writer.WriteElementString("Value", (string)genericAttribute.Value);
                _writer.WriteElementString("StoreId", entity.StoreId.ToString());
                _writer.WriteEndElement();
            }
            _writer.WriteEndElement();
        }

        public void WriteAddress(dynamic address, string node)
        {
            if (address == null)
                return;

            Address entity = address.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            _writer.WriteElementString("Id", entity.Id.ToString());
            _writer.WriteElementString("Salutation", entity.Salutation);
            _writer.WriteElementString("Title", entity.Title);
            _writer.WriteElementString("FirstName", entity.FirstName);
            _writer.WriteElementString("LastName", entity.LastName);
            _writer.WriteElementString("Email", entity.Email);
            _writer.WriteElementString("Company", entity.Company);
            _writer.WriteElementString("CountryId", entity.CountryId.HasValue ? entity.CountryId.Value.ToString() : string.Empty);
            _writer.WriteElementString("StateProvinceId", entity.StateProvinceId.HasValue ? entity.StateProvinceId.Value.ToString() : string.Empty);
            _writer.WriteElementString("City", entity.City);
            _writer.WriteElementString("Address1", entity.Address1);
            _writer.WriteElementString("Address2", entity.Address2);
            _writer.WriteElementString("ZipPostalCode", entity.ZipPostalCode);
            _writer.WriteElementString("PhoneNumber", entity.PhoneNumber);
            _writer.WriteElementString("FaxNumber", entity.FaxNumber);
            _writer.WriteElementString("CreatedOnUtc", entity.CreatedOnUtc.ToString(_culture));

            if (address.Country != null)
            {
                dynamic country = address.Country;
                Country entityCountry = address.Country.Entity;

                _writer.WriteStartElement("Country");
                _writer.WriteElementString("Id", entityCountry.Id.ToString());
                _writer.WriteElementString("Name", (string)country.Name);
                _writer.WriteElementString("AllowsBilling", entityCountry.AllowsBilling.ToString());
                _writer.WriteElementString("AllowsShipping", entityCountry.AllowsShipping.ToString());
                _writer.WriteElementString("TwoLetterIsoCode", entityCountry.TwoLetterIsoCode);
                _writer.WriteElementString("ThreeLetterIsoCode", entityCountry.ThreeLetterIsoCode);
                _writer.WriteElementString("NumericIsoCode", entityCountry.NumericIsoCode.ToString());
                _writer.WriteElementString("SubjectToVat", entityCountry.SubjectToVat.ToString());
                _writer.WriteElementString("Published", entityCountry.Published.ToString());
                _writer.WriteElementString("DisplayOrder", entityCountry.DisplayOrder.ToString());
                _writer.WriteElementString("LimitedToStores", entityCountry.LimitedToStores.ToString());

                WriteLocalized(country);
                _writer.WriteEndElement();
            }

            if (address.StateProvince != null)
            {
                dynamic stateProvince = address.StateProvince;
                StateProvince entityStateProvince = address.StateProvince.Entity;

                _writer.WriteStartElement("StateProvince");
                _writer.WriteElementString("Id", entityStateProvince.Id.ToString());
                _writer.WriteElementString("CountryId", entityStateProvince.CountryId.ToString());
                _writer.WriteElementString("Name", (string)stateProvince.Name);
                _writer.WriteElementString("Abbreviation", (string)stateProvince.Abbreviation);
                _writer.WriteElementString("Published", entityStateProvince.Published.ToString());
                _writer.WriteElementString("DisplayOrder", entityStateProvince.DisplayOrder.ToString());

                WriteLocalized(stateProvince);
                _writer.WriteEndElement();
            }

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteCurrency(dynamic currency, string node)
        {
            if (currency == null)
                return;

            Currency entity = currency.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            _writer.WriteElementString("Id", entity.Id.ToString());
            _writer.WriteElementString("Name", (string)currency.Name);
            _writer.WriteElementString("CurrencyCode", entity.CurrencyCode);
            _writer.WriteElementString("Rate", entity.Rate.ToString(_culture));
            _writer.WriteElementString("DisplayLocale", entity.DisplayLocale);
            _writer.WriteElementString("CustomFormatting", entity.CustomFormatting);
            _writer.WriteElementString("LimitedToStores", entity.LimitedToStores.ToString());
            _writer.WriteElementString("Published", entity.Published.ToString());
            _writer.WriteElementString("DisplayOrder", entity.DisplayOrder.ToString());
            _writer.WriteElementString("CreatedOnUtc", entity.CreatedOnUtc.ToString(_culture));
            _writer.WriteElementString("UpdatedOnUtc", entity.UpdatedOnUtc.ToString(_culture));
            _writer.WriteElementString("DomainEndings", entity.DomainEndings);
            _writer.WriteElementString("RoundOrderItemsEnabled", entity.RoundOrderItemsEnabled.ToString());
            _writer.WriteElementString("RoundNumDecimals", entity.RoundNumDecimals.ToString());
            _writer.WriteElementString("RoundOrderTotalEnabled", entity.RoundOrderTotalEnabled.ToString());
            _writer.WriteElementString("RoundOrderTotalDenominator", entity.RoundOrderTotalDenominator.ToString(_culture));
            _writer.WriteElementString("RoundOrderTotalRule", ((int)entity.RoundOrderTotalRule).ToString());

            WriteLocalized(currency);

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteCountry(dynamic country, string node)
        {
            if (country == null)
                return;

            Country entity = country.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            _writer.WriteElementString("Id", entity.Id.ToString());
            _writer.WriteElementString("Name", entity.Name);
            _writer.WriteElementString("AllowsBilling", entity.AllowsBilling.ToString());
            _writer.WriteElementString("AllowsShipping", entity.AllowsShipping.ToString());
            _writer.WriteElementString("TwoLetterIsoCode", entity.TwoLetterIsoCode);
            _writer.WriteElementString("ThreeLetterIsoCode", entity.ThreeLetterIsoCode);
            _writer.WriteElementString("NumericIsoCode", entity.NumericIsoCode.ToString());
            _writer.WriteElementString("SubjectToVat", entity.SubjectToVat.ToString());
            _writer.WriteElementString("Published", entity.Published.ToString());
            _writer.WriteElementString("DisplayOrder", entity.DisplayOrder.ToString());
            _writer.WriteElementString("LimitedToStores", entity.LimitedToStores.ToString());

            WriteLocalized(country);

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteRewardPointsHistory(dynamic rewardPoints, string node)
        {
            if (rewardPoints == null)
                return;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            foreach (dynamic rewardPoint in rewardPoints)
            {
                RewardPointsHistory entity = rewardPoint.Entity;

                _writer.WriteStartElement("RewardPointsHistory");
                _writer.WriteElementString("Id", entity.ToString());
                _writer.WriteElementString("CustomerId", entity.ToString());
                _writer.WriteElementString("Points", entity.Points.ToString());
                _writer.WriteElementString("PointsBalance", entity.PointsBalance.ToString());
                _writer.WriteElementString("UsedAmount", entity.UsedAmount.ToString(_culture));
                _writer.WriteElementString("Message", (string)rewardPoint.Message);
                _writer.WriteElementString("CreatedOnUtc", entity.CreatedOnUtc.ToString(_culture));
                _writer.WriteEndElement();
            }

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteDeliveryTime(dynamic deliveryTime, string node)
        {
            if (deliveryTime == null)
                return;

            DeliveryTime entity = deliveryTime.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            _writer.WriteElementString("Id", entity.Id.ToString());
            _writer.WriteElementString("Name", (string)deliveryTime.Name);
            _writer.WriteElementString("DisplayLocale", entity.DisplayLocale);
            _writer.WriteElementString("ColorHexValue", entity.ColorHexValue);
            _writer.WriteElementString("DisplayOrder", entity.DisplayOrder.ToString());
            _writer.WriteElementString("IsDefault", entity.IsDefault.ToString());
            _writer.WriteElementString("MinDays", entity.MinDays.HasValue ? entity.MinDays.Value.ToString() : string.Empty);
            _writer.WriteElementString("MaxDays", entity.MaxDays.HasValue ? entity.MaxDays.Value.ToString() : string.Empty);

            WriteLocalized(deliveryTime);

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteQuantityUnit(dynamic quantityUnit, string node)
        {
            if (quantityUnit == null)
                return;

            QuantityUnit entity = quantityUnit.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            _writer.WriteElementString("Id", entity.Id.ToString());
            _writer.WriteElementString("Name", (string)quantityUnit.Name);
            _writer.WriteElementString("NamePlural", (string)quantityUnit.NamePlural);
            _writer.WriteElementString("Description", (string)quantityUnit.Description);
            _writer.WriteElementString("DisplayLocale", entity.DisplayLocale);
            _writer.WriteElementString("DisplayOrder", entity.DisplayOrder.ToString());
            _writer.WriteElementString("IsDefault", entity.IsDefault.ToString());

            WriteLocalized(quantityUnit);

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WritePicture(dynamic picture, string node)
        {
            if (picture == null)
                return;

            MediaFile entity = picture.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            var seoName = (string)picture.Name;
            seoName = Path.GetFileNameWithoutExtension(seoName.EmptyNull());

            _writer.WriteElementString("Id", entity.Id.ToString());
            _writer.WriteElementString("SeoFilename", seoName);
            _writer.WriteElementString("MimeType", (string)picture.MimeType);
            _writer.WriteElementString("ThumbImageUrl", (string)picture._ThumbImageUrl);
            _writer.WriteElementString("ImageUrl", (string)picture._ImageUrl);
            _writer.WriteElementString("FullSizeImageUrl", (string)picture._FullSizeImageUrl);
            _writer.WriteElementString("FileName", (string)picture._FileName);

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteCategory(dynamic category, string node)
        {
            if (category == null)
                return;

            Category entity = category.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            _writer.WriteElementString("Id", entity.Id.ToString());

            if (!Exclude.HasFlag(ExportXmlExclude.Category))
            {
                _writer.WriteElementString("Name", (string)category.Name);
                _writer.WriteElementString("FullName", (string)category.FullName);
                _writer.WriteElementString("Description", ((string)category.Description).RemoveInvalidXmlChars());
                _writer.WriteElementString("BottomDescription", ((string)category.BottomDescription).RemoveInvalidXmlChars());
                _writer.WriteElementString("CategoryTemplateId", entity.CategoryTemplateId.ToString());
                _writer.WriteElementString("CategoryTemplateViewPath", (string)category._CategoryTemplateViewPath);
                _writer.WriteElementString("MetaKeywords", (string)category.MetaKeywords);
                _writer.WriteElementString("MetaDescription", (string)category.MetaDescription);
                _writer.WriteElementString("MetaTitle", (string)category.MetaTitle);
                _writer.WriteElementString("SeName", (string)category.SeName);
                _writer.WriteElementString("ParentCategoryId", entity.ParentCategoryId.ToString());
                _writer.WriteElementString("PictureId", entity.MediaFileId.ToString());
                _writer.WriteElementString("PageSize", entity.PageSize.ToString());
                _writer.WriteElementString("AllowCustomersToSelectPageSize", entity.AllowCustomersToSelectPageSize.ToString());
                _writer.WriteElementString("PageSizeOptions", entity.PageSizeOptions);
                _writer.WriteElementString("ShowOnHomePage", entity.ShowOnHomePage.ToString());
                _writer.WriteElementString("HasDiscountsApplied", entity.HasDiscountsApplied.ToString());
                _writer.WriteElementString("Published", entity.Published.ToString());
                _writer.WriteElementString("Deleted", entity.Deleted.ToString());
                _writer.WriteElementString("DisplayOrder", entity.DisplayOrder.ToString());
                _writer.WriteElementString("CreatedOnUtc", entity.CreatedOnUtc.ToString(_culture));
                _writer.WriteElementString("UpdatedOnUtc", entity.UpdatedOnUtc.ToString(_culture));
                _writer.WriteElementString("SubjectToAcl", entity.SubjectToAcl.ToString());
                _writer.WriteElementString("LimitedToStores", entity.LimitedToStores.ToString());
                _writer.WriteElementString("Alias", (string)category.Alias);
                _writer.WriteElementString("DefaultViewMode", entity.DefaultViewMode);

                WritePicture(category.Picture, "Picture");
                WriteLocalized(category);
            }

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteManufacturer(dynamic manufacturer, string node)
        {
            if (manufacturer == null)
                return;

            Manufacturer entity = manufacturer.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            _writer.WriteElementString("Id", entity.Id.ToString());
            _writer.WriteElementString("Name", (string)manufacturer.Name);
            _writer.WriteElementString("SeName", (string)manufacturer.SeName);
            _writer.WriteElementString("Description", ((string)manufacturer.Description).RemoveInvalidXmlChars());
            _writer.WriteElementString("BottomDescription", ((string)manufacturer.BottomDescription).RemoveInvalidXmlChars());
            _writer.WriteElementString("ManufacturerTemplateId", entity.ManufacturerTemplateId.ToString());
            _writer.WriteElementString("MetaKeywords", (string)manufacturer.MetaKeywords);
            _writer.WriteElementString("MetaDescription", (string)manufacturer.MetaDescription);
            _writer.WriteElementString("MetaTitle", (string)manufacturer.MetaTitle);
            _writer.WriteElementString("PictureId", entity.MediaFileId.ToString());
            _writer.WriteElementString("PageSize", entity.PageSize.ToString());
            _writer.WriteElementString("AllowCustomersToSelectPageSize", entity.AllowCustomersToSelectPageSize.ToString());
            _writer.WriteElementString("PageSizeOptions", entity.PageSizeOptions);
            _writer.WriteElementString("Published", entity.Published.ToString());
            _writer.WriteElementString("Deleted", entity.Deleted.ToString());
            _writer.WriteElementString("DisplayOrder", entity.DisplayOrder.ToString());
            _writer.WriteElementString("CreatedOnUtc", entity.CreatedOnUtc.ToString(_culture));
            _writer.WriteElementString("UpdatedOnUtc", entity.UpdatedOnUtc.ToString(_culture));
            _writer.WriteElementString("HasDiscountsApplied", entity.HasDiscountsApplied.ToString());
            _writer.WriteElementString("SubjectToAcl", entity.SubjectToAcl.ToString());
            _writer.WriteElementString("LimitedToStores", entity.LimitedToStores.ToString());

            WritePicture(manufacturer.Picture, "Picture");
            WriteLocalized(manufacturer);

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteCustomer(dynamic customer, string node)
        {
            if (customer == null)
                return;

            Customer entity = customer.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            _writer.WriteElementString("Id", entity.Id.ToString());
            _writer.WriteElementString("CustomerGuid", entity.CustomerGuid.ToString());
            _writer.WriteElementString("Username", entity.Username);
            _writer.WriteElementString("Email", entity.Email);
            _writer.WriteElementString("AdminComment", entity.AdminComment);
            _writer.WriteElementString("IsTaxExempt", entity.IsTaxExempt.ToString());
            _writer.WriteElementString("AffiliateId", entity.AffiliateId.ToString());
            _writer.WriteElementString("Active", entity.Active.ToString());
            _writer.WriteElementString("Deleted", entity.Deleted.ToString());
            _writer.WriteElementString("IsSystemAccount", entity.IsSystemAccount.ToString());
            _writer.WriteElementString("SystemName", entity.SystemName);
            _writer.WriteElementString("LastIpAddress", entity.LastIpAddress);
            _writer.WriteElementString("CreatedOnUtc", entity.CreatedOnUtc.ToString(_culture));
            _writer.WriteElementString("LastLoginDateUtc", entity.LastLoginDateUtc.HasValue ? entity.LastLoginDateUtc.Value.ToString(_culture) : string.Empty);
            _writer.WriteElementString("LastActivityDateUtc", entity.LastActivityDateUtc.ToString(_culture));
            _writer.WriteElementString("Salutation", entity.Salutation);
            _writer.WriteElementString("Title", entity.Title);
            _writer.WriteElementString("FirstName", entity.FirstName);
            _writer.WriteElementString("LastName", entity.LastName);
            _writer.WriteElementString("FullName", entity.FullName);
            _writer.WriteElementString("Company", entity.Company);
            _writer.WriteElementString("CustomerNumber", entity.CustomerNumber);
            _writer.WriteElementString("BirthDate", entity.BirthDate.HasValue ? entity.BirthDate.Value.ToString(_culture) : string.Empty);
            _writer.WriteElementString("RewardPointsBalance", ((int)customer._RewardPointsBalance).ToString());

            if (customer.CustomerRoles != null)
            {
                _writer.WriteStartElement("CustomerRoles");
                foreach (dynamic role in customer.CustomerRoles)
                {
                    CustomerRole entityRole = role.Entity;

                    _writer.WriteStartElement("CustomerRole");
                    _writer.WriteElementString("Id", entityRole.Id.ToString());
                    _writer.WriteElementString("Name", (string)role.Name);
                    _writer.WriteElementString("FreeShipping", entityRole.FreeShipping.ToString());
                    _writer.WriteElementString("TaxExempt", entityRole.TaxExempt.ToString());
                    _writer.WriteElementString("TaxDisplayType", entityRole.TaxDisplayType.HasValue ? entityRole.TaxDisplayType.Value.ToString() : string.Empty);
                    _writer.WriteElementString("Active", entityRole.Active.ToString());
                    _writer.WriteElementString("IsSystemRole", entityRole.IsSystemRole.ToString());
                    _writer.WriteElementString("SystemName", entityRole.SystemName);
                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            WriteRewardPointsHistory(customer.RewardPointsHistory, "RewardPointsHistories");
            WriteAddress(customer.BillingAddress, "BillingAddress");
            WriteAddress(customer.ShippingAddress, "ShippingAddress");

            if (customer.Addresses != null)
            {
                _writer.WriteStartElement("Addresses");
                foreach (dynamic address in customer.Addresses)
                {
                    WriteAddress(address, "Address");
                }
                _writer.WriteEndElement();
            }

            WriteGenericAttributes(customer);

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteShoppingCartItem(dynamic shoppingCartItem, string node)
        {
            if (shoppingCartItem == null)
                return;

            ShoppingCartItem entity = shoppingCartItem.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            _writer.WriteElementString("Id", entity.Id.ToString());
            _writer.WriteElementString("StoreId", entity.StoreId.ToString());
            _writer.WriteElementString("ParentItemId", entity.ParentItemId.HasValue ? entity.ParentItemId.Value.ToString() : string.Empty);
            _writer.WriteElementString("BundleItemId", entity.BundleItemId.HasValue ? entity.BundleItemId.Value.ToString() : string.Empty);
            _writer.WriteElementString("ShoppingCartTypeId", entity.ShoppingCartTypeId.ToString());
            _writer.WriteElementString("CustomerId", entity.CustomerId.ToString());
            _writer.WriteElementString("ProductId", entity.ProductId.ToString());
            _writer.WriteCData("AttributesXml", entity.RawAttributes);
            _writer.WriteElementString("CustomerEnteredPrice", entity.CustomerEnteredPrice.ToString(_culture));
            _writer.WriteElementString("Quantity", entity.Quantity.ToString());
            _writer.WriteElementString("CreatedOnUtc", entity.CreatedOnUtc.ToString(_culture));
            _writer.WriteElementString("UpdatedOnUtc", entity.UpdatedOnUtc.ToString(_culture));

            WriteCustomer(shoppingCartItem.Customer, "Customer");
            WriteProduct(shoppingCartItem.Product, "Product");

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteProduct(dynamic product, string node)
        {
            if (product == null)
                return;

            Product entity = product.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            decimal? basePriceAmount = product.BasePriceAmount;
            int? basePriceBaseAmount = product.BasePriceBaseAmount;
            decimal? lowestAttributeCombinationPrice = product.LowestAttributeCombinationPrice;

            _writer.WriteElementString("Id", entity.Id.ToString());
            _writer.WriteElementString("Name", (string)product.Name);
            _writer.WriteElementString("SeName", (string)product.SeName);
            _writer.WriteElementString("ShortDescription", (string)product.ShortDescription);
            _writer.WriteElementString("FullDescription", ((string)product.FullDescription).RemoveInvalidXmlChars());
            _writer.WriteElementString("AdminComment", (string)product.AdminComment);
            _writer.WriteElementString("ProductTemplateId", entity.ProductTemplateId.ToString());
            _writer.WriteElementString("ProductTemplateViewPath", (string)product._ProductTemplateViewPath);
            _writer.WriteElementString("ShowOnHomePage", entity.ShowOnHomePage.ToString());
            _writer.WriteElementString("HomePageDisplayOrder", entity.HomePageDisplayOrder.ToString());
            _writer.WriteElementString("MetaKeywords", (string)product.MetaKeywords);
            _writer.WriteElementString("MetaDescription", (string)product.MetaDescription);
            _writer.WriteElementString("MetaTitle", (string)product.MetaTitle);
            _writer.WriteElementString("AllowCustomerReviews", entity.AllowCustomerReviews.ToString());
            _writer.WriteElementString("ApprovedRatingSum", entity.ApprovedRatingSum.ToString());
            _writer.WriteElementString("NotApprovedRatingSum", entity.NotApprovedRatingSum.ToString());
            _writer.WriteElementString("ApprovedTotalReviews", entity.ApprovedTotalReviews.ToString());
            _writer.WriteElementString("NotApprovedTotalReviews", entity.NotApprovedTotalReviews.ToString());
            _writer.WriteElementString("Published", entity.Published.ToString());
            _writer.WriteElementString("CreatedOnUtc", entity.CreatedOnUtc.ToString(_culture));
            _writer.WriteElementString("UpdatedOnUtc", entity.UpdatedOnUtc.ToString(_culture));
            _writer.WriteElementString("SubjectToAcl", entity.SubjectToAcl.ToString());
            _writer.WriteElementString("LimitedToStores", entity.LimitedToStores.ToString());
            _writer.WriteElementString("ProductTypeId", entity.ProductTypeId.ToString());
            _writer.WriteElementString("ParentGroupedProductId", entity.ParentGroupedProductId.ToString());
            _writer.WriteElementString("Sku", (string)product.Sku);
            _writer.WriteElementString("ManufacturerPartNumber", (string)product.ManufacturerPartNumber);
            _writer.WriteElementString("Gtin", (string)product.Gtin);
            _writer.WriteElementString("IsGiftCard", entity.IsGiftCard.ToString());
            _writer.WriteElementString("GiftCardTypeId", entity.GiftCardTypeId.ToString());
            _writer.WriteElementString("RequireOtherProducts", entity.RequireOtherProducts.ToString());
            _writer.WriteElementString("RequiredProductIds", entity.RequiredProductIds);
            _writer.WriteElementString("AutomaticallyAddRequiredProducts", entity.AutomaticallyAddRequiredProducts.ToString());
            _writer.WriteElementString("IsDownload", entity.IsDownload.ToString());
            _writer.WriteElementString("UnlimitedDownloads", entity.UnlimitedDownloads.ToString());
            _writer.WriteElementString("MaxNumberOfDownloads", entity.MaxNumberOfDownloads.ToString());
            _writer.WriteElementString("DownloadExpirationDays", entity.DownloadExpirationDays.HasValue ? entity.DownloadExpirationDays.Value.ToString() : string.Empty);
            _writer.WriteElementString("DownloadActivationTypeId", entity.DownloadActivationTypeId.ToString());
            _writer.WriteElementString("HasSampleDownload", entity.HasSampleDownload.ToString());
            _writer.WriteElementString("SampleDownloadId", entity.SampleDownloadId.HasValue ? entity.SampleDownloadId.Value.ToString() : string.Empty);
            _writer.WriteElementString("HasUserAgreement", entity.HasUserAgreement.ToString());
            _writer.WriteElementString("UserAgreementText", entity.UserAgreementText);
            _writer.WriteElementString("IsRecurring", entity.IsRecurring.ToString());
            _writer.WriteElementString("RecurringCycleLength", entity.RecurringCycleLength.ToString());
            _writer.WriteElementString("RecurringCyclePeriodId", entity.RecurringCyclePeriodId.ToString());
            _writer.WriteElementString("RecurringTotalCycles", entity.RecurringTotalCycles.ToString());
            _writer.WriteElementString("IsShipEnabled", entity.IsShippingEnabled.ToString());
            _writer.WriteElementString("IsFreeShipping", entity.IsFreeShipping.ToString());
            _writer.WriteElementString("AdditionalShippingCharge", entity.AdditionalShippingCharge.ToString(_culture));
            _writer.WriteElementString("IsTaxExempt", entity.IsTaxExempt.ToString());
            _writer.WriteElementString("TaxCategoryId", entity.TaxCategoryId.ToString());
            _writer.WriteElementString("ManageInventoryMethodId", entity.ManageInventoryMethodId.ToString());
            _writer.WriteElementString("StockQuantity", entity.StockQuantity.ToString());
            _writer.WriteElementString("DisplayStockAvailability", entity.DisplayStockAvailability.ToString());
            _writer.WriteElementString("DisplayStockQuantity", entity.DisplayStockQuantity.ToString());
            _writer.WriteElementString("MinStockQuantity", entity.MinStockQuantity.ToString());
            _writer.WriteElementString("LowStockActivityId", entity.LowStockActivityId.ToString());
            _writer.WriteElementString("NotifyAdminForQuantityBelow", entity.NotifyAdminForQuantityBelow.ToString());
            _writer.WriteElementString("BackorderModeId", entity.BackorderModeId.ToString());
            _writer.WriteElementString("AllowBackInStockSubscriptions", entity.AllowBackInStockSubscriptions.ToString());
            _writer.WriteElementString("OrderMinimumQuantity", entity.OrderMinimumQuantity.ToString());
            _writer.WriteElementString("OrderMaximumQuantity", entity.OrderMaximumQuantity.ToString());
            _writer.WriteElementString("QuantityStep", entity.QuantityStep.ToString());
            _writer.WriteElementString("QuantityControlType", ((int)entity.QuantityControlType).ToString());
            _writer.WriteElementString("HideQuantityControl", entity.HideQuantityControl.ToString());
            _writer.WriteElementString("AllowedQuantities", entity.AllowedQuantities);
            _writer.WriteElementString("DisableBuyButton", entity.DisableBuyButton.ToString());
            _writer.WriteElementString("DisableWishlistButton", entity.DisableWishlistButton.ToString());
            _writer.WriteElementString("AvailableForPreOrder", entity.AvailableForPreOrder.ToString());
            _writer.WriteElementString("CallForPrice", entity.CallForPrice.ToString());
            _writer.WriteElementString("Price", entity.Price.ToString(_culture));
            _writer.WriteElementString("OldPrice", entity.OldPrice.ToString(_culture));
            _writer.WriteElementString("ProductCost", entity.ProductCost.ToString(_culture));
            _writer.WriteElementString("SpecialPrice", entity.SpecialPrice.HasValue ? entity.SpecialPrice.Value.ToString(_culture) : string.Empty);
            _writer.WriteElementString("SpecialPriceStartDateTimeUtc", entity.SpecialPriceStartDateTimeUtc.HasValue ? entity.SpecialPriceStartDateTimeUtc.Value.ToString(_culture) : string.Empty);
            _writer.WriteElementString("SpecialPriceEndDateTimeUtc", entity.SpecialPriceEndDateTimeUtc.HasValue ? entity.SpecialPriceEndDateTimeUtc.Value.ToString(_culture) : string.Empty);
            _writer.WriteElementString("CustomerEntersPrice", entity.CustomerEntersPrice.ToString());
            _writer.WriteElementString("MinimumCustomerEnteredPrice", entity.MinimumCustomerEnteredPrice.ToString(_culture));
            _writer.WriteElementString("MaximumCustomerEnteredPrice", entity.MaximumCustomerEnteredPrice.ToString(_culture));
            _writer.WriteElementString("HasTierPrices", entity.HasTierPrices.ToString());
            _writer.WriteElementString("HasDiscountsApplied", entity.HasDiscountsApplied.ToString());
            _writer.WriteElementString("MainPictureId", entity.MainPictureId.HasValue ? entity.MainPictureId.Value.ToString() : string.Empty);
            _writer.WriteElementString("Weight", ((decimal)product.Weight).ToString(_culture));
            _writer.WriteElementString("Length", ((decimal)product.Length).ToString(_culture));
            _writer.WriteElementString("Width", ((decimal)product.Width).ToString(_culture));
            _writer.WriteElementString("Height", ((decimal)product.Height).ToString(_culture));
            _writer.WriteElementString("AvailableStartDateTimeUtc", entity.AvailableStartDateTimeUtc.HasValue ? entity.AvailableStartDateTimeUtc.Value.ToString(_culture) : string.Empty);
            _writer.WriteElementString("AvailableEndDateTimeUtc", entity.AvailableEndDateTimeUtc.HasValue ? entity.AvailableEndDateTimeUtc.Value.ToString(_culture) : string.Empty);
            _writer.WriteElementString("BasePriceEnabled", ((bool)product.BasePriceEnabled).ToString());
            _writer.WriteElementString("BasePriceMeasureUnit", (string)product.BasePriceMeasureUnit);
            _writer.WriteElementString("BasePriceAmount", basePriceAmount.HasValue ? basePriceAmount.Value.ToString(_culture) : string.Empty);
            _writer.WriteElementString("BasePriceBaseAmount", basePriceBaseAmount.HasValue ? basePriceBaseAmount.Value.ToString() : string.Empty);
            _writer.WriteElementString("BasePriceHasValue", ((bool)product.BasePriceHasValue).ToString());
            _writer.WriteElementString("BasePriceInfo", (string)product._BasePriceInfo);
            _writer.WriteElementString("Visibility", ((int)entity.Visibility).ToString());
            _writer.WriteElementString("Condition", ((int)entity.Condition).ToString());
            _writer.WriteElementString("DisplayOrder", entity.DisplayOrder.ToString());
            _writer.WriteElementString("IsSystemProduct", entity.IsSystemProduct.ToString());
            _writer.WriteElementString("BundleTitleText", entity.BundleTitleText);
            _writer.WriteElementString("BundlePerItemPricing", entity.BundlePerItemPricing.ToString());
            _writer.WriteElementString("BundlePerItemShipping", entity.BundlePerItemShipping.ToString());
            _writer.WriteElementString("BundlePerItemShoppingCart", entity.BundlePerItemShoppingCart.ToString());
            _writer.WriteElementString("LowestAttributeCombinationPrice", lowestAttributeCombinationPrice.HasValue ? lowestAttributeCombinationPrice.Value.ToString(_culture) : string.Empty);
            _writer.WriteElementString("AttributeChoiceBehaviour", ((int)entity.AttributeChoiceBehaviour).ToString());
            _writer.WriteElementString("IsEsd", entity.IsEsd.ToString());
            _writer.WriteElementString("CustomsTariffNumber", entity.CustomsTariffNumber);

            WriteLocalized(product);
            WriteDeliveryTime(product.DeliveryTime, "DeliveryTime");
            WriteQuantityUnit(product.QuantityUnit, "QuantityUnit");
            WriteCountry(product.CountryOfOrigin, "CountryOfOrigin");
            WriteAttributes(product);

            if (product.AppliedDiscounts != null)
            {
                _writer.WriteStartElement("AppliedDiscounts");
                foreach (dynamic discount in product.AppliedDiscounts)
                {
                    Discount entityDiscount = discount.Entity;

                    _writer.WriteStartElement("AppliedDiscount");
                    _writer.WriteElementString("Id", entityDiscount.Id.ToString());
                    _writer.WriteElementString("Name", (string)discount.Name);
                    _writer.WriteElementString("DiscountTypeId", entityDiscount.DiscountTypeId.ToString());
                    _writer.WriteElementString("UsePercentage", entityDiscount.UsePercentage.ToString());
                    _writer.WriteElementString("DiscountPercentage", entityDiscount.DiscountPercentage.ToString(_culture));
                    _writer.WriteElementString("DiscountAmount", entityDiscount.DiscountAmount.ToString(_culture));
                    _writer.WriteElementString("StartDateUtc", entityDiscount.StartDateUtc.HasValue ? entityDiscount.StartDateUtc.Value.ToString(_culture) : string.Empty);
                    _writer.WriteElementString("EndDateUtc", entityDiscount.EndDateUtc.HasValue ? entityDiscount.EndDateUtc.Value.ToString(_culture) : string.Empty);
                    _writer.WriteElementString("RequiresCouponCode", entityDiscount.RequiresCouponCode.ToString());
                    _writer.WriteElementString("CouponCode", entityDiscount.CouponCode);
                    _writer.WriteElementString("DiscountLimitationId", entityDiscount.DiscountLimitationId.ToString());
                    _writer.WriteElementString("LimitationTimes", entityDiscount.LimitationTimes.ToString());
                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            if (product.Downloads != null)
            {
                _writer.WriteStartElement("Downloads");
                foreach (dynamic download in product.Downloads)
                {
                    Download downloadEntity = download.Entity;
                    var mediaFile = downloadEntity.MediaFile;

                    _writer.WriteStartElement("Download");
                    _writer.WriteElementString("Id", downloadEntity.Id.ToString());
                    _writer.WriteElementString("DownloadGuid", downloadEntity.DownloadGuid.ToString());
                    _writer.WriteElementString("UseDownloadUrl", downloadEntity.UseDownloadUrl.ToString());
                    _writer.WriteElementString("DownloadUrl", downloadEntity.DownloadUrl);
                    _writer.WriteElementString("IsTransient", downloadEntity.IsTransient.ToString());
                    _writer.WriteElementString("UpdatedOnUtc", downloadEntity.UpdatedOnUtc.ToString(_culture));
                    _writer.WriteElementString("EntityId", downloadEntity.EntityId.ToString());
                    _writer.WriteElementString("EntityName", downloadEntity.EntityName);
                    _writer.WriteElementString("FileVersion", downloadEntity.FileVersion);
                    _writer.WriteElementString("Changelog", downloadEntity.Changelog);
                    if (!downloadEntity.UseDownloadUrl && mediaFile != null)
                    {
                        _writer.WriteElementString("ContentType", mediaFile.MimeType);
                        _writer.WriteElementString("Filename", mediaFile.Name);
                        _writer.WriteElementString("Extension", mediaFile.Extension);
                        _writer.WriteElementString("MediaStorageId", mediaFile.MediaStorageId.HasValue ? mediaFile.MediaStorageId.Value.ToString() : string.Empty);
                    }
                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            if (product.TierPrices != null)
            {
                _writer.WriteStartElement("TierPrices");
                foreach (dynamic tierPrice in product.TierPrices)
                {
                    TierPrice entityTierPrice = tierPrice.Entity;

                    _writer.WriteStartElement("TierPrice");
                    _writer.WriteElementString("Id", entityTierPrice.Id.ToString());
                    _writer.WriteElementString("ProductId", entityTierPrice.ProductId.ToString());
                    _writer.WriteElementString("StoreId", entityTierPrice.StoreId.ToString());
                    _writer.WriteElementString("CustomerRoleId", entityTierPrice.CustomerRoleId.HasValue ? entityTierPrice.CustomerRoleId.Value.ToString() : string.Empty);
                    _writer.WriteElementString("Quantity", entityTierPrice.Quantity.ToString());
                    _writer.WriteElementString("Price", entityTierPrice.Price.ToString(_culture));
                    _writer.WriteElementString("CalculationMethod", ((int)entityTierPrice.CalculationMethod).ToString());
                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            if (product.ProductTags != null)
            {
                _writer.WriteStartElement("ProductTags");
                foreach (dynamic tag in product.ProductTags)
                {
                    ProductTag entityTag = tag.Entity;

                    _writer.WriteStartElement("ProductTag");
                    _writer.WriteElementString("Id", ((int)tag.Id).ToString());
                    _writer.WriteElementString("Name", (string)tag.Name);
                    _writer.WriteElementString("SeName", (string)tag.SeName);
                    _writer.WriteElementString("Published", entityTag.Published.ToString());

                    WriteLocalized(tag);

                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            if (product.ProductPictures != null)
            {
                _writer.WriteStartElement("ProductPictures");
                foreach (dynamic productPicture in product.ProductPictures)
                {
                    ProductMediaFile entityProductPicture = productPicture.Entity;

                    _writer.WriteStartElement("ProductPicture");
                    _writer.WriteElementString("Id", entityProductPicture.Id.ToString());
                    _writer.WriteElementString("DisplayOrder", entityProductPicture.DisplayOrder.ToString());

                    WritePicture(productPicture.Picture, "Picture");
                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            if (product.ProductCategories != null)
            {
                _writer.WriteStartElement("ProductCategories");
                foreach (dynamic productCategory in product.ProductCategories)
                {
                    ProductCategory entityProductCategory = productCategory.Entity;

                    _writer.WriteStartElement("ProductCategory");
                    _writer.WriteElementString("Id", entityProductCategory.Id.ToString());
                    _writer.WriteElementString("DisplayOrder", entityProductCategory.DisplayOrder.ToString());
                    _writer.WriteElementString("IsFeaturedProduct", entityProductCategory.IsFeaturedProduct.ToString());

                    WriteCategory(productCategory.Category, "Category");
                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            if (product.ProductManufacturers != null)
            {
                _writer.WriteStartElement("ProductManufacturers");
                foreach (dynamic productManu in product.ProductManufacturers)
                {
                    ProductManufacturer entityProductManu = productManu.Entity;

                    _writer.WriteStartElement("ProductManufacturer");
                    _writer.WriteElementString("Id", entityProductManu.Id.ToString());
                    _writer.WriteElementString("DisplayOrder", entityProductManu.DisplayOrder.ToString());
                    _writer.WriteElementString("IsFeaturedProduct", entityProductManu.IsFeaturedProduct.ToString());

                    WriteManufacturer(productManu.Manufacturer, "Manufacturer");
                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            if (product.ProductBundleItems != null)
            {
                _writer.WriteStartElement("ProductBundleItems");
                foreach (dynamic bundleItem in product.ProductBundleItems)
                {
                    ProductBundleItem entityPbi = bundleItem.Entity;

                    _writer.WriteStartElement("ProductBundleItem");
                    _writer.WriteElementString("Id", entityPbi.Id.ToString());
                    _writer.WriteElementString("ProductId", entityPbi.ProductId.ToString());
                    _writer.WriteElementString("BundleProductId", entityPbi.BundleProductId.ToString());
                    _writer.WriteElementString("Quantity", entityPbi.Quantity.ToString());
                    _writer.WriteElementString("Discount", entityPbi.Discount.HasValue ? entityPbi.Discount.Value.ToString(_culture) : string.Empty);
                    _writer.WriteElementString("DiscountPercentage", entityPbi.DiscountPercentage.ToString());
                    _writer.WriteElementString("Name", (string)bundleItem.Name);
                    _writer.WriteElementString("ShortDescription", (string)bundleItem.ShortDescription);
                    _writer.WriteElementString("FilterAttributes", entityPbi.FilterAttributes.ToString());
                    _writer.WriteElementString("HideThumbnail", entityPbi.HideThumbnail.ToString());
                    _writer.WriteElementString("Visible", entityPbi.Visible.ToString());
                    _writer.WriteElementString("Published", entityPbi.Published.ToString());
                    _writer.WriteElementString("DisplayOrder", ((int)bundleItem.DisplayOrder).ToString());
                    _writer.WriteElementString("CreatedOnUtc", entityPbi.CreatedOnUtc.ToString(_culture));
                    _writer.WriteElementString("UpdatedOnUtc", entityPbi.UpdatedOnUtc.ToString(_culture));

                    WriteLocalized(bundleItem);
                    _writer.WriteEndElement();  // ProductBundleItem
                }
                _writer.WriteEndElement();  // ProductBundleItems
            }

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        private void WriteAttributes(dynamic product)
        {
            if (product.ProductAttributes != null)
            {
                _writer.WriteStartElement("ProductAttributes");
                foreach (dynamic pva in product.ProductAttributes)
                {
                    ProductVariantAttribute entityPva = pva.Entity;
                    ProductAttribute entityPa = pva.Attribute.Entity;

                    _writer.WriteStartElement("ProductAttribute");
                    _writer.WriteElementString("Id", entityPva.Id.ToString());
                    _writer.WriteElementString("TextPrompt", (string)pva.TextPrompt);
                    _writer.WriteElementString("IsRequired", entityPva.IsRequired.ToString());
                    _writer.WriteElementString("AttributeControlTypeId", entityPva.AttributeControlTypeId.ToString());
                    _writer.WriteElementString("DisplayOrder", entityPva.DisplayOrder.ToString());

                    _writer.WriteStartElement("Attribute");
                    _writer.WriteElementString("Id", entityPa.Id.ToString());
                    _writer.WriteElementString("Alias", entityPa.Alias);
                    _writer.WriteElementString("Name", entityPa.Name);
                    _writer.WriteElementString("Description", entityPa.Description);
                    _writer.WriteElementString("AllowFiltering", entityPa.AllowFiltering.ToString());
                    _writer.WriteElementString("DisplayOrder", entityPa.DisplayOrder.ToString());
                    _writer.WriteElementString("FacetTemplateHint", ((int)entityPa.FacetTemplateHint).ToString());
                    _writer.WriteElementString("IndexOptionNames", entityPa.IndexOptionNames.ToString());

                    WriteLocalized(pva.Attribute);
                    _writer.WriteEndElement();  // Attribute

                    _writer.WriteStartElement("AttributeValues");
                    foreach (dynamic value in pva.Attribute.Values)
                    {
                        ProductVariantAttributeValue entityPvav = value.Entity;

                        _writer.WriteStartElement("AttributeValue");
                        _writer.WriteElementString("Id", entityPvav.Id.ToString());
                        _writer.WriteElementString("Alias", (string)value.Alias);
                        _writer.WriteElementString("Name", (string)value.Name);
                        _writer.WriteElementString("Color", (string)value.Color);
                        _writer.WriteElementString("PriceAdjustment", ((decimal)value.PriceAdjustment).ToString(_culture));
                        _writer.WriteElementString("WeightAdjustment", ((decimal)value.WeightAdjustment).ToString(_culture));
                        _writer.WriteElementString("IsPreSelected", entityPvav.IsPreSelected.ToString());
                        _writer.WriteElementString("DisplayOrder", entityPvav.DisplayOrder.ToString());
                        _writer.WriteElementString("ValueTypeId", entityPvav.ValueTypeId.ToString());
                        _writer.WriteElementString("LinkedProductId", entityPvav.LinkedProductId.ToString());
                        _writer.WriteElementString("Quantity", entityPvav.Quantity.ToString());

                        WriteLocalized(value);
                        _writer.WriteEndElement();  // AttributeValue
                    }
                    _writer.WriteEndElement();  // AttributeValues
                    _writer.WriteEndElement();  // ProductAttribute
                }
                _writer.WriteEndElement();  // ProductAttributes
            }

            if (product.ProductAttributeCombinations != null)
            {
                _writer.WriteStartElement("ProductAttributeCombinations");
                foreach (dynamic combination in product.ProductAttributeCombinations)
                {
                    ProductVariantAttributeCombination entityPvac = combination.Entity;

                    _writer.WriteStartElement("ProductAttributeCombination");
                    _writer.WriteElementString("Id", entityPvac.Id.ToString());
                    _writer.WriteElementString("StockQuantity", entityPvac.StockQuantity.ToString());
                    _writer.WriteElementString("AllowOutOfStockOrders", entityPvac.AllowOutOfStockOrders.ToString());
                    _writer.WriteElementString("AttributesXml", entityPvac.RawAttributes);
                    _writer.WriteElementString("Sku", entityPvac.Sku);
                    _writer.WriteElementString("Gtin", entityPvac.Gtin);
                    _writer.WriteElementString("ManufacturerPartNumber", entityPvac.ManufacturerPartNumber);
                    _writer.WriteElementString("Price", entityPvac.Price.HasValue ? entityPvac.Price.Value.ToString(_culture) : string.Empty);
                    _writer.WriteElementString("Length", entityPvac.Length.HasValue ? entityPvac.Length.Value.ToString(_culture) : string.Empty);
                    _writer.WriteElementString("Width", entityPvac.Width.HasValue ? entityPvac.Width.Value.ToString(_culture) : string.Empty);
                    _writer.WriteElementString("Height", entityPvac.Height.HasValue ? entityPvac.Height.Value.ToString(_culture) : string.Empty);
                    _writer.WriteElementString("BasePriceAmount", entityPvac.BasePriceAmount.HasValue ? entityPvac.BasePriceAmount.Value.ToString(_culture) : string.Empty);
                    _writer.WriteElementString("BasePriceBaseAmount", entityPvac.BasePriceBaseAmount.HasValue ? entityPvac.BasePriceBaseAmount.Value.ToString() : string.Empty);
                    _writer.WriteElementString("AssignedPictureIds", entityPvac.AssignedMediaFileIds);
                    _writer.WriteElementString("IsActive", entityPvac.IsActive.ToString());

                    WriteDeliveryTime(combination.DeliveryTime, "DeliveryTime");
                    WriteQuantityUnit(combination.QuantityUnit, "QuantityUnit");

                    _writer.WriteStartElement("Pictures");
                    foreach (dynamic assignedPicture in combination.Pictures)
                    {
                        WritePicture(assignedPicture, "Picture");
                    }
                    _writer.WriteEndElement();  // Pictures
                    _writer.WriteEndElement();  // ProductAttributeCombination
                }
                _writer.WriteEndElement(); // ProductAttributeCombinations
            }

            if (product.ProductSpecificationAttributes != null)
            {
                _writer.WriteStartElement("ProductSpecificationAttributes");
                foreach (dynamic psa in product.ProductSpecificationAttributes)
                {
                    ProductSpecificationAttribute entityPsa = psa.Entity;

                    _writer.WriteStartElement("ProductSpecificationAttribute");
                    _writer.WriteElementString("Id", entityPsa.Id.ToString());
                    _writer.WriteElementString("ProductId", entityPsa.ProductId.ToString());
                    _writer.WriteElementString("SpecificationAttributeOptionId", entityPsa.SpecificationAttributeOptionId.ToString());
                    _writer.WriteElementString("AllowFiltering", entityPsa.AllowFiltering.ToString());
                    _writer.WriteElementString("ShowOnProductPage", entityPsa.ShowOnProductPage.ToString());
                    _writer.WriteElementString("DisplayOrder", entityPsa.DisplayOrder.ToString());

                    dynamic option = psa.SpecificationAttributeOption;
                    SpecificationAttributeOption entitySao = option.Entity;
                    SpecificationAttribute entitySa = option.SpecificationAttribute.Entity;

                    _writer.WriteStartElement("SpecificationAttributeOption");
                    _writer.WriteElementString("Id", entitySao.Id.ToString());
                    _writer.WriteElementString("SpecificationAttributeId", entitySao.SpecificationAttributeId.ToString());
                    _writer.WriteElementString("DisplayOrder", entitySao.DisplayOrder.ToString());
                    _writer.WriteElementString("NumberValue", ((decimal)option.NumberValue).ToString(_culture));
                    _writer.WriteElementString("Color", (string)option.Color);
                    _writer.WriteElementString("Name", (string)option.Name);
                    _writer.WriteElementString("Alias", (string)option.Alias);

                    WriteLocalized(option);

                    _writer.WriteStartElement("SpecificationAttribute");
                    _writer.WriteElementString("Id", entitySa.Id.ToString());
                    _writer.WriteElementString("Name", (string)option.SpecificationAttribute.Name);
                    _writer.WriteElementString("Alias", (string)option.SpecificationAttribute.Alias);
                    _writer.WriteElementString("DisplayOrder", entitySa.DisplayOrder.ToString());
                    _writer.WriteElementString("AllowFiltering", entitySa.AllowFiltering.ToString());
                    _writer.WriteElementString("ShowOnProductPage", entitySa.ShowOnProductPage.ToString());
                    _writer.WriteElementString("FacetSorting", ((int)entitySa.FacetSorting).ToString());
                    _writer.WriteElementString("FacetTemplateHint", ((int)entitySa.FacetTemplateHint).ToString());
                    _writer.WriteElementString("IndexOptionNames", entitySa.IndexOptionNames.ToString());

                    WriteLocalized(option.SpecificationAttribute);

                    _writer.WriteEndElement();  // SpecificationAttribute
                    _writer.WriteEndElement();  // SpecificationAttributeOption

                    _writer.WriteEndElement();  // ProductSpecificationAttribute
                }
                _writer.WriteEndElement();  // ProductSpecificationAttributes
            }
        }


        protected override void OnDispose(bool disposing)
        {
            if (_writer != null && !_doNotDispose)
            {
                _writer.Dispose();
            }
        }

        protected override async ValueTask OnDisposeAsync(bool disposing)
        {
            if (_writer != null && !_doNotDispose)
            {
                await _writer.DisposeAsync();
            }
        }
    }
}
