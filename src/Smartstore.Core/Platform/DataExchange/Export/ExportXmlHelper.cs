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
                _writer.WriteElementString(nameof(GenericAttribute.Id), entity.Id.ToString());
                _writer.WriteElementString(nameof(GenericAttribute.EntityId), entity.EntityId.ToString());
                _writer.WriteElementString(nameof(GenericAttribute.KeyGroup), entity.KeyGroup);
                _writer.WriteElementString(nameof(GenericAttribute.Key), entity.Key);
                _writer.WriteElementString(nameof(GenericAttribute.Value), (string)genericAttribute.Value);
                _writer.WriteElementString(nameof(GenericAttribute.StoreId), entity.StoreId.ToString());
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

            _writer.WriteElementString(nameof(Address.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(Address.Salutation), entity.Salutation);
            _writer.WriteElementString(nameof(Address.Title), entity.Title);
            _writer.WriteElementString(nameof(Address.FirstName), entity.FirstName);
            _writer.WriteElementString(nameof(Address.LastName), entity.LastName);
            _writer.WriteElementString(nameof(Address.Email), entity.Email);
            _writer.WriteElementString(nameof(Address.Company), entity.Company);
            _writer.WriteElementString(nameof(Address.CountryId), entity.CountryId?.ToString() ?? string.Empty);
            _writer.WriteElementString(nameof(Address.StateProvinceId), entity.StateProvinceId?.ToString() ?? string.Empty);
            _writer.WriteElementString(nameof(Address.City), entity.City);
            _writer.WriteElementString(nameof(Address.Address1), entity.Address1);
            _writer.WriteElementString(nameof(Address.Address2), entity.Address2);
            _writer.WriteElementString(nameof(Address.ZipPostalCode), entity.ZipPostalCode);
            _writer.WriteElementString(nameof(Address.PhoneNumber), entity.PhoneNumber);
            _writer.WriteElementString(nameof(Address.FaxNumber), entity.FaxNumber);
            _writer.WriteElementString(nameof(Address.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));

            if (address.Country != null)
            {
                dynamic country = address.Country;
                Country entityCountry = address.Country.Entity;

                _writer.WriteStartElement("Country");
                _writer.WriteElementString(nameof(Country.Id), entityCountry.Id.ToString());
                _writer.WriteElementString(nameof(Country.Name), (string)country.Name);
                _writer.WriteElementString(nameof(Country.AllowsBilling), entityCountry.AllowsBilling.ToString());
                _writer.WriteElementString(nameof(Country.AllowsShipping), entityCountry.AllowsShipping.ToString());
                _writer.WriteElementString(nameof(Country.TwoLetterIsoCode), entityCountry.TwoLetterIsoCode);
                _writer.WriteElementString(nameof(Country.ThreeLetterIsoCode), entityCountry.ThreeLetterIsoCode);
                _writer.WriteElementString(nameof(Country.NumericIsoCode), entityCountry.NumericIsoCode.ToString());
                _writer.WriteElementString(nameof(Country.SubjectToVat), entityCountry.SubjectToVat.ToString());
                _writer.WriteElementString(nameof(Country.Published), entityCountry.Published.ToString());
                _writer.WriteElementString(nameof(Country.DisplayOrder), entityCountry.DisplayOrder.ToString());
                _writer.WriteElementString(nameof(Country.LimitedToStores), entityCountry.LimitedToStores.ToString());

                WriteLocalized(country);
                _writer.WriteEndElement();
            }

            if (address.StateProvince != null)
            {
                dynamic stateProvince = address.StateProvince;
                StateProvince entityStateProvince = address.StateProvince.Entity;

                _writer.WriteStartElement("StateProvince");
                _writer.WriteElementString(nameof(StateProvince.Id), entityStateProvince.Id.ToString());
                _writer.WriteElementString(nameof(StateProvince.CountryId), entityStateProvince.CountryId.ToString());
                _writer.WriteElementString(nameof(StateProvince.Name), (string)stateProvince.Name);
                _writer.WriteElementString(nameof(StateProvince.Abbreviation), (string)stateProvince.Abbreviation);
                _writer.WriteElementString(nameof(StateProvince.Published), entityStateProvince.Published.ToString());
                _writer.WriteElementString(nameof(StateProvince.DisplayOrder), entityStateProvince.DisplayOrder.ToString());

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

            _writer.WriteElementString(nameof(Currency.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(Currency.Name), (string)currency.Name);
            _writer.WriteElementString(nameof(Currency.CurrencyCode), entity.CurrencyCode);
            _writer.WriteElementString(nameof(Currency.Rate), entity.Rate.ToString(_culture));
            _writer.WriteElementString(nameof(Currency.DisplayLocale), entity.DisplayLocale);
            _writer.WriteElementString(nameof(Currency.CustomFormatting), entity.CustomFormatting);
            _writer.WriteElementString(nameof(Currency.LimitedToStores), entity.LimitedToStores.ToString());
            _writer.WriteElementString(nameof(Currency.Published), entity.Published.ToString());
            _writer.WriteElementString(nameof(Currency.DisplayOrder), entity.DisplayOrder.ToString());
            _writer.WriteElementString(nameof(Currency.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));
            _writer.WriteElementString(nameof(Currency.UpdatedOnUtc), entity.UpdatedOnUtc.ToString(_culture));
            _writer.WriteElementString(nameof(Currency.DomainEndings), entity.DomainEndings);
            _writer.WriteElementString(nameof(Currency.RoundNumDecimals), entity.RoundNumDecimals.ToString());
            _writer.WriteElementString(nameof(Currency.MidpointRounding), ((int)entity.MidpointRounding).ToString());
            _writer.WriteElementString(nameof(Currency.RoundOrderItemsEnabled), entity.RoundOrderItemsEnabled.ToString());
            _writer.WriteElementString(nameof(Currency.RoundNetPrices), entity.RoundNetPrices.ToString());
            _writer.WriteElementString(nameof(Currency.RoundUnitPrices), entity.RoundUnitPrices.ToString());
            _writer.WriteElementString(nameof(Currency.RoundOrderTotalEnabled), entity.RoundOrderTotalEnabled.ToString());
            _writer.WriteElementString(nameof(Currency.RoundOrderTotalDenominator), entity.RoundOrderTotalDenominator.ToString(_culture));
            _writer.WriteElementString(nameof(Currency.RoundOrderTotalRule), ((int)entity.RoundOrderTotalRule).ToString());

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

            _writer.WriteElementString(nameof(Country.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(Country.Name), entity.Name);
            _writer.WriteElementString(nameof(Country.AllowsBilling), entity.AllowsBilling.ToString());
            _writer.WriteElementString(nameof(Country.AllowsShipping), entity.AllowsShipping.ToString());
            _writer.WriteElementString(nameof(Country.TwoLetterIsoCode), entity.TwoLetterIsoCode);
            _writer.WriteElementString(nameof(Country.ThreeLetterIsoCode), entity.ThreeLetterIsoCode);
            _writer.WriteElementString(nameof(Country.NumericIsoCode), entity.NumericIsoCode.ToString());
            _writer.WriteElementString(nameof(Country.SubjectToVat), entity.SubjectToVat.ToString());
            _writer.WriteElementString(nameof(Country.Published), entity.Published.ToString());
            _writer.WriteElementString(nameof(Country.DisplayOrder), entity.DisplayOrder.ToString());
            _writer.WriteElementString(nameof(Country.LimitedToStores), entity.LimitedToStores.ToString());

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
                _writer.WriteElementString(nameof(RewardPointsHistory.Id), entity.ToString());
                _writer.WriteElementString(nameof(RewardPointsHistory.CustomerId), entity.ToString());
                _writer.WriteElementString(nameof(RewardPointsHistory.Points), entity.Points.ToString());
                _writer.WriteElementString(nameof(RewardPointsHistory.PointsBalance), entity.PointsBalance.ToString());
                _writer.WriteElementString(nameof(RewardPointsHistory.UsedAmount), entity.UsedAmount.ToString(_culture));
                _writer.WriteElementString(nameof(RewardPointsHistory.Message), (string)rewardPoint.Message);
                _writer.WriteElementString(nameof(RewardPointsHistory.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));
                _writer.WriteEndElement();
            }

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WritePriceLabel(dynamic priceLabel, string node)
        {
            if (priceLabel == null)
                return;

            PriceLabel entity = priceLabel.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            _writer.WriteElementString(nameof(PriceLabel.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(PriceLabel.ShortName), (string)priceLabel.ShortName);
            _writer.WriteElementString(nameof(PriceLabel.Name), (string)priceLabel.Name);
            _writer.WriteElementString(nameof(PriceLabel.Description), (string)priceLabel.Description);
            _writer.WriteElementString(nameof(PriceLabel.IsRetailPrice), entity.IsRetailPrice.ToString());
            _writer.WriteElementString(nameof(PriceLabel.DisplayShortNameInLists), entity.DisplayShortNameInLists.ToString());
            _writer.WriteElementString(nameof(PriceLabel.DisplayOrder), entity.DisplayOrder.ToString());

            WriteLocalized(priceLabel);

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

            _writer.WriteElementString(nameof(DeliveryTime.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(DeliveryTime.Name), (string)deliveryTime.Name);
            _writer.WriteElementString(nameof(DeliveryTime.DisplayLocale), entity.DisplayLocale);
            _writer.WriteElementString(nameof(DeliveryTime.ColorHexValue), entity.ColorHexValue);
            _writer.WriteElementString(nameof(DeliveryTime.DisplayOrder), entity.DisplayOrder.ToString());
            _writer.WriteElementString(nameof(DeliveryTime.IsDefault), entity.IsDefault.ToString());
            _writer.WriteElementString(nameof(DeliveryTime.MinDays), entity.MinDays?.ToString() ?? string.Empty);
            _writer.WriteElementString(nameof(DeliveryTime.MaxDays), entity.MaxDays?.ToString() ?? string.Empty);

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

            _writer.WriteElementString(nameof(QuantityUnit.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(QuantityUnit.Name), (string)quantityUnit.Name);
            _writer.WriteElementString(nameof(QuantityUnit.NamePlural), (string)quantityUnit.NamePlural);
            _writer.WriteElementString(nameof(QuantityUnit.Description), (string)quantityUnit.Description);
            _writer.WriteElementString(nameof(QuantityUnit.DisplayLocale), entity.DisplayLocale);
            _writer.WriteElementString(nameof(QuantityUnit.DisplayOrder), entity.DisplayOrder.ToString());
            _writer.WriteElementString(nameof(QuantityUnit.IsDefault), entity.IsDefault.ToString());

            WriteLocalized(quantityUnit);

            if (node.HasValue())
            {
                _writer.WriteEndElement();
            }
        }

        public void WriteMediaFile(dynamic file, string node)
        {
            if (file == null)
                return;

            MediaFile entity = file.Entity;

            if (node.HasValue())
            {
                _writer.WriteStartElement(node);
            }

            var seoName = (string)file.Name;
            seoName = Path.GetFileNameWithoutExtension(seoName.EmptyNull());

            _writer.WriteElementString(nameof(MediaFile.Id), entity.Id.ToString());
            _writer.WriteElementString("SeoFilename", seoName);
            _writer.WriteElementString(nameof(MediaFile.MimeType), (string)file.MimeType);
            _writer.WriteElementString("ThumbImageUrl", (string)file._ThumbImageUrl);
            _writer.WriteElementString("ImageUrl", (string)file._ImageUrl);
            _writer.WriteElementString("FullSizeImageUrl", (string)file._FullSizeImageUrl);
            _writer.WriteElementString("FileName", (string)file._FileName);

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

            _writer.WriteElementString(nameof(Category.Id), entity.Id.ToString());

            if (!Exclude.HasFlag(ExportXmlExclude.Category))
            {
                _writer.WriteElementString(nameof(Category.Name), (string)category.Name);
                _writer.WriteElementString(nameof(Category.FullName), (string)category.FullName);
                _writer.WriteElementString(nameof(Category.Description), ((string)category.Description).RemoveInvalidXmlChars());
                _writer.WriteElementString(nameof(Category.BottomDescription), ((string)category.BottomDescription).RemoveInvalidXmlChars());
                _writer.WriteElementString(nameof(Category.CategoryTemplateId), entity.CategoryTemplateId.ToString());
                _writer.WriteElementString("CategoryTemplateViewPath", (string)category._CategoryTemplateViewPath);
                _writer.WriteElementString(nameof(Category.MetaKeywords), (string)category.MetaKeywords);
                _writer.WriteElementString(nameof(Category.MetaDescription), (string)category.MetaDescription);
                _writer.WriteElementString(nameof(Category.MetaTitle), (string)category.MetaTitle);
                _writer.WriteElementString("SeName", (string)category.SeName);
                _writer.WriteElementString(nameof(Category.ParentId), entity.ParentId.GetValueOrDefault().ToStringInvariant());
                _writer.WriteElementString(nameof(Category.MediaFileId), entity.MediaFileId.ToString());
                _writer.WriteElementString(nameof(Category.PageSize), entity.PageSize.ToString());
                _writer.WriteElementString(nameof(Category.AllowCustomersToSelectPageSize), entity.AllowCustomersToSelectPageSize.ToString());
                _writer.WriteElementString(nameof(Category.PageSizeOptions), entity.PageSizeOptions);
                _writer.WriteElementString(nameof(Category.ShowOnHomePage), entity.ShowOnHomePage.ToString());
                _writer.WriteElementString(nameof(Category.HasDiscountsApplied), entity.HasDiscountsApplied.ToString());
                _writer.WriteElementString(nameof(Category.Published), entity.Published.ToString());
                _writer.WriteElementString(nameof(Category.Deleted), entity.Deleted.ToString());
                _writer.WriteElementString(nameof(Category.DisplayOrder), entity.DisplayOrder.ToString());
                _writer.WriteElementString(nameof(Category.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));
                _writer.WriteElementString(nameof(Category.UpdatedOnUtc), entity.UpdatedOnUtc.ToString(_culture));
                _writer.WriteElementString(nameof(Category.SubjectToAcl), entity.SubjectToAcl.ToString());
                _writer.WriteElementString(nameof(Category.LimitedToStores), entity.LimitedToStores.ToString());
                _writer.WriteElementString(nameof(Category.Alias), (string)category.Alias);
                _writer.WriteElementString(nameof(Category.DefaultViewMode), entity.DefaultViewMode);

                WriteMediaFile(category.File, "File");
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

            _writer.WriteElementString(nameof(Manufacturer.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(Manufacturer.Name), (string)manufacturer.Name);
            _writer.WriteElementString("SeName", (string)manufacturer.SeName);
            _writer.WriteElementString(nameof(Manufacturer.Description), ((string)manufacturer.Description).RemoveInvalidXmlChars());
            _writer.WriteElementString(nameof(Manufacturer.BottomDescription), ((string)manufacturer.BottomDescription).RemoveInvalidXmlChars());
            _writer.WriteElementString(nameof(Manufacturer.ManufacturerTemplateId), entity.ManufacturerTemplateId.ToString());
            _writer.WriteElementString(nameof(Manufacturer.MetaKeywords), (string)manufacturer.MetaKeywords);
            _writer.WriteElementString(nameof(Manufacturer.MetaDescription), (string)manufacturer.MetaDescription);
            _writer.WriteElementString(nameof(Manufacturer.MetaTitle), (string)manufacturer.MetaTitle);
            _writer.WriteElementString(nameof(Manufacturer.MediaFileId), entity.MediaFileId.ToString());
            _writer.WriteElementString(nameof(Manufacturer.PageSize), entity.PageSize.ToString());
            _writer.WriteElementString(nameof(Manufacturer.AllowCustomersToSelectPageSize), entity.AllowCustomersToSelectPageSize.ToString());
            _writer.WriteElementString(nameof(Manufacturer.PageSizeOptions), entity.PageSizeOptions);
            _writer.WriteElementString(nameof(Manufacturer.Published), entity.Published.ToString());
            _writer.WriteElementString(nameof(Manufacturer.Deleted), entity.Deleted.ToString());
            _writer.WriteElementString(nameof(Manufacturer.DisplayOrder), entity.DisplayOrder.ToString());
            _writer.WriteElementString(nameof(Manufacturer.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));
            _writer.WriteElementString(nameof(Manufacturer.UpdatedOnUtc), entity.UpdatedOnUtc.ToString(_culture));
            _writer.WriteElementString(nameof(Manufacturer.HasDiscountsApplied), entity.HasDiscountsApplied.ToString());
            _writer.WriteElementString(nameof(Manufacturer.SubjectToAcl), entity.SubjectToAcl.ToString());
            _writer.WriteElementString(nameof(Manufacturer.LimitedToStores), entity.LimitedToStores.ToString());

            WriteMediaFile(manufacturer.File, "File");
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

            _writer.WriteElementString(nameof(Customer.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(Customer.CustomerGuid), entity.CustomerGuid.ToString());
            _writer.WriteElementString(nameof(Customer.Username), entity.Username);
            _writer.WriteElementString(nameof(Customer.Email), entity.Email);
            _writer.WriteElementString(nameof(Customer.AdminComment), entity.AdminComment);
            _writer.WriteElementString(nameof(Customer.IsTaxExempt), entity.IsTaxExempt.ToString());
            _writer.WriteElementString(nameof(Customer.AffiliateId), entity.AffiliateId.ToString());
            _writer.WriteElementString(nameof(Customer.Active), entity.Active.ToString());
            _writer.WriteElementString(nameof(Customer.Deleted), entity.Deleted.ToString());
            _writer.WriteElementString(nameof(Customer.IsSystemAccount), entity.IsSystemAccount.ToString());
            _writer.WriteElementString(nameof(Customer.SystemName), entity.SystemName);
            _writer.WriteElementString(nameof(Customer.LastIpAddress), entity.LastIpAddress);
            _writer.WriteElementString(nameof(Customer.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));
            _writer.WriteElementString(nameof(Customer.LastLoginDateUtc), entity.LastLoginDateUtc?.ToString(_culture) ?? string.Empty);
            _writer.WriteElementString(nameof(Customer.LastActivityDateUtc), entity.LastActivityDateUtc.ToString(_culture));
            _writer.WriteElementString(nameof(Customer.Salutation), entity.Salutation);
            _writer.WriteElementString(nameof(Customer.Title), entity.Title);
            _writer.WriteElementString(nameof(Customer.FirstName), entity.FirstName);
            _writer.WriteElementString(nameof(Customer.LastName), entity.LastName);
            _writer.WriteElementString(nameof(Customer.FullName), entity.FullName);
            _writer.WriteElementString(nameof(Customer.Company), entity.Company);
            _writer.WriteElementString(nameof(Customer.CustomerNumber), entity.CustomerNumber);
            _writer.WriteElementString(nameof(Customer.BirthDate), entity.BirthDate?.ToString(_culture) ?? string.Empty);
            _writer.WriteElementString("RewardPointsBalance", ((int)customer._RewardPointsBalance).ToString());

            if (customer.CustomerRoles != null)
            {
                _writer.WriteStartElement("CustomerRoles");
                foreach (dynamic role in customer.CustomerRoles)
                {
                    CustomerRole entityRole = role.Entity;

                    _writer.WriteStartElement("CustomerRole");
                    _writer.WriteElementString(nameof(CustomerRole.Id), entityRole.Id.ToString());
                    _writer.WriteElementString(nameof(CustomerRole.Name), (string)role.Name);
                    _writer.WriteElementString(nameof(CustomerRole.FreeShipping), entityRole.FreeShipping.ToString());
                    _writer.WriteElementString(nameof(CustomerRole.TaxExempt), entityRole.TaxExempt.ToString());
                    _writer.WriteElementString(nameof(CustomerRole.TaxDisplayType), entityRole.TaxDisplayType?.ToString() ?? string.Empty);
                    _writer.WriteElementString(nameof(CustomerRole.Active), entityRole.Active.ToString());
                    _writer.WriteElementString(nameof(CustomerRole.IsSystemRole), entityRole.IsSystemRole.ToString());
                    _writer.WriteElementString(nameof(CustomerRole.SystemName), entityRole.SystemName);
                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            WriteRewardPointsHistory(customer.RewardPointsHistory, "RewardPointsHistories");
            WriteAddress(customer.BillingAddress, nameof(Customer.BillingAddress));
            WriteAddress(customer.ShippingAddress, nameof(Customer.ShippingAddress));

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

            _writer.WriteElementString(nameof(ShoppingCartItem.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(ShoppingCartItem.Active), entity.Active.ToString());
            _writer.WriteElementString(nameof(ShoppingCartItem.StoreId), entity.StoreId.ToString());
            _writer.WriteElementString(nameof(ShoppingCartItem.ParentItemId), entity.ParentItemId?.ToString() ?? string.Empty);
            _writer.WriteElementString(nameof(ShoppingCartItem.BundleItemId), entity.BundleItemId?.ToString() ?? string.Empty);
            _writer.WriteElementString(nameof(ShoppingCartItem.ShoppingCartTypeId), entity.ShoppingCartTypeId.ToString());
            _writer.WriteElementString(nameof(ShoppingCartItem.CustomerId), entity.CustomerId.ToString());
            _writer.WriteElementString(nameof(ShoppingCartItem.ProductId), entity.ProductId.ToString());
            _writer.WriteCData(nameof(ShoppingCartItem.RawAttributes), entity.RawAttributes);
            _writer.WriteElementString(nameof(ShoppingCartItem.CustomerEnteredPrice), entity.CustomerEnteredPrice.ToString(_culture));
            _writer.WriteElementString(nameof(ShoppingCartItem.Quantity), entity.Quantity.ToString());
            _writer.WriteElementString(nameof(ShoppingCartItem.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));
            _writer.WriteElementString(nameof(ShoppingCartItem.UpdatedOnUtc), entity.UpdatedOnUtc.ToString(_culture));

            WriteCustomer(shoppingCartItem.Customer, nameof(ShoppingCartItem.Customer));
            WriteProduct(shoppingCartItem.Product, nameof(ShoppingCartItem.Product));

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

            _writer.WriteElementString(nameof(Product.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(Product.Name), (string)product.Name);
            _writer.WriteElementString("SeName", (string)product.SeName);
            _writer.WriteElementString(nameof(Product.ShortDescription), (string)product.ShortDescription);
            _writer.WriteElementString(nameof(Product.FullDescription), ((string)product.FullDescription).RemoveInvalidXmlChars());
            _writer.WriteElementString(nameof(Product.AdminComment), (string)product.AdminComment);
            _writer.WriteElementString(nameof(Product.ProductTemplateId), entity.ProductTemplateId.ToString());
            _writer.WriteElementString("ProductTemplateViewPath", (string)product._ProductTemplateViewPath);
            _writer.WriteElementString(nameof(Product.ShowOnHomePage), entity.ShowOnHomePage.ToString());
            _writer.WriteElementString(nameof(Product.HomePageDisplayOrder), entity.HomePageDisplayOrder.ToString());
            _writer.WriteElementString(nameof(Product.MetaKeywords), (string)product.MetaKeywords);
            _writer.WriteElementString(nameof(Product.MetaDescription), (string)product.MetaDescription);
            _writer.WriteElementString(nameof(Product.MetaTitle), (string)product.MetaTitle);
            _writer.WriteElementString(nameof(Product.AllowCustomerReviews), entity.AllowCustomerReviews.ToString());
            _writer.WriteElementString(nameof(Product.ApprovedRatingSum), entity.ApprovedRatingSum.ToString());
            _writer.WriteElementString(nameof(Product.NotApprovedRatingSum), entity.NotApprovedRatingSum.ToString());
            _writer.WriteElementString(nameof(Product.ApprovedTotalReviews), entity.ApprovedTotalReviews.ToString());
            _writer.WriteElementString(nameof(Product.NotApprovedTotalReviews), entity.NotApprovedTotalReviews.ToString());
            _writer.WriteElementString(nameof(Product.Published), entity.Published.ToString());
            _writer.WriteElementString(nameof(Product.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));
            _writer.WriteElementString(nameof(Product.UpdatedOnUtc), entity.UpdatedOnUtc.ToString(_culture));
            _writer.WriteElementString(nameof(Product.SubjectToAcl), entity.SubjectToAcl.ToString());
            _writer.WriteElementString(nameof(Product.LimitedToStores), entity.LimitedToStores.ToString());
            _writer.WriteElementString(nameof(Product.ProductTypeId), entity.ProductTypeId.ToString());
            _writer.WriteElementString(nameof(Product.ParentGroupedProductId), entity.ParentGroupedProductId.ToString());
            _writer.WriteElementString(nameof(Product.Sku), (string)product.Sku);
            _writer.WriteElementString(nameof(Product.ManufacturerPartNumber), (string)product.ManufacturerPartNumber);
            _writer.WriteElementString(nameof(Product.Gtin), (string)product.Gtin);
            _writer.WriteElementString(nameof(Product.IsGiftCard), entity.IsGiftCard.ToString());
            _writer.WriteElementString(nameof(Product.GiftCardTypeId), entity.GiftCardTypeId.ToString());
            _writer.WriteElementString(nameof(Product.RequireOtherProducts), entity.RequireOtherProducts.ToString());
            _writer.WriteElementString(nameof(Product.RequiredProductIds), entity.RequiredProductIds);
            _writer.WriteElementString(nameof(Product.AutomaticallyAddRequiredProducts), entity.AutomaticallyAddRequiredProducts.ToString());
            _writer.WriteElementString(nameof(Product.IsDownload), entity.IsDownload.ToString());
            _writer.WriteElementString(nameof(Product.UnlimitedDownloads), entity.UnlimitedDownloads.ToString());
            _writer.WriteElementString(nameof(Product.MaxNumberOfDownloads), entity.MaxNumberOfDownloads.ToString());
            _writer.WriteElementString(nameof(Product.DownloadExpirationDays), entity.DownloadExpirationDays?.ToString() ?? string.Empty);
            _writer.WriteElementString(nameof(Product.DownloadActivationTypeId), entity.DownloadActivationTypeId.ToString());
            _writer.WriteElementString(nameof(Product.HasSampleDownload), entity.HasSampleDownload.ToString());
            _writer.WriteElementString(nameof(Product.SampleDownloadId), entity.SampleDownloadId?.ToString() ?? string.Empty);
            _writer.WriteElementString(nameof(Product.HasUserAgreement), entity.HasUserAgreement.ToString());
            _writer.WriteElementString(nameof(Product.UserAgreementText), entity.UserAgreementText);
            _writer.WriteElementString(nameof(Product.IsRecurring), entity.IsRecurring.ToString());
            _writer.WriteElementString(nameof(Product.RecurringCycleLength), entity.RecurringCycleLength.ToString());
            _writer.WriteElementString(nameof(Product.RecurringCyclePeriodId), entity.RecurringCyclePeriodId.ToString());
            _writer.WriteElementString(nameof(Product.RecurringTotalCycles), entity.RecurringTotalCycles.ToString());
            _writer.WriteElementString(nameof(Product.IsShippingEnabled), entity.IsShippingEnabled.ToString());
            _writer.WriteElementString(nameof(Product.IsFreeShipping), entity.IsFreeShipping.ToString());
            _writer.WriteElementString(nameof(Product.AdditionalShippingCharge), entity.AdditionalShippingCharge.ToString(_culture));
            _writer.WriteElementString(nameof(Product.IsTaxExempt), entity.IsTaxExempt.ToString());
            _writer.WriteElementString(nameof(Product.TaxCategoryId), entity.TaxCategoryId.ToString());
            _writer.WriteElementString(nameof(Product.ManageInventoryMethodId), entity.ManageInventoryMethodId.ToString());
            _writer.WriteElementString(nameof(Product.StockQuantity), ((int)product.StockQuantity).ToString());
            _writer.WriteElementString(nameof(Product.DisplayStockAvailability), entity.DisplayStockAvailability.ToString());
            _writer.WriteElementString(nameof(Product.DisplayStockQuantity), entity.DisplayStockQuantity.ToString());
            _writer.WriteElementString(nameof(Product.MinStockQuantity), entity.MinStockQuantity.ToString());
            _writer.WriteElementString(nameof(Product.LowStockActivityId), entity.LowStockActivityId.ToString());
            _writer.WriteElementString(nameof(Product.NotifyAdminForQuantityBelow), entity.NotifyAdminForQuantityBelow.ToString());
            _writer.WriteElementString(nameof(Product.BackorderModeId), ((int)product.BackorderModeId).ToString());
            _writer.WriteElementString(nameof(Product.AllowBackInStockSubscriptions), entity.AllowBackInStockSubscriptions.ToString());
            _writer.WriteElementString(nameof(Product.OrderMinimumQuantity), entity.OrderMinimumQuantity.ToString());
            _writer.WriteElementString(nameof(Product.OrderMaximumQuantity), entity.OrderMaximumQuantity.ToString());
            _writer.WriteElementString(nameof(Product.QuantityStep), entity.QuantityStep.ToString());
            _writer.WriteElementString(nameof(Product.QuantityControlType), ((int)entity.QuantityControlType).ToString());
            _writer.WriteElementString(nameof(Product.HideQuantityControl), entity.HideQuantityControl.ToString());
            _writer.WriteElementString(nameof(Product.AllowedQuantities), entity.AllowedQuantities);
            _writer.WriteElementString(nameof(Product.DisableBuyButton), entity.DisableBuyButton.ToString());
            _writer.WriteElementString(nameof(Product.DisableWishlistButton), entity.DisableWishlistButton.ToString());
            _writer.WriteElementString(nameof(Product.AvailableForPreOrder), entity.AvailableForPreOrder.ToString());
            _writer.WriteElementString(nameof(Product.CallForPrice), entity.CallForPrice.ToString());
            _writer.WriteElementString(nameof(Product.Price), ((decimal)product.Price).ToString(_culture));
            _writer.WriteElementString(nameof(Product.ComparePrice), entity.ComparePrice.ToString(_culture));
            _writer.WriteElementString(nameof(Product.ProductCost), entity.ProductCost.ToString(_culture));
            _writer.WriteElementString(nameof(Product.SpecialPrice), entity.SpecialPrice?.ToString(_culture) ?? string.Empty);
            _writer.WriteElementString(nameof(Product.SpecialPriceStartDateTimeUtc), entity.SpecialPriceStartDateTimeUtc?.ToString(_culture) ?? string.Empty);
            _writer.WriteElementString(nameof(Product.SpecialPriceEndDateTimeUtc), entity.SpecialPriceEndDateTimeUtc?.ToString(_culture) ?? string.Empty);
            _writer.WriteElementString(nameof(Product.CustomerEntersPrice), entity.CustomerEntersPrice.ToString());
            _writer.WriteElementString(nameof(Product.MinimumCustomerEnteredPrice), entity.MinimumCustomerEnteredPrice.ToString(_culture));
            _writer.WriteElementString(nameof(Product.MaximumCustomerEnteredPrice), entity.MaximumCustomerEnteredPrice.ToString(_culture));
            _writer.WriteElementString(nameof(Product.HasTierPrices), entity.HasTierPrices.ToString());
            _writer.WriteElementString(nameof(Product.HasDiscountsApplied), entity.HasDiscountsApplied.ToString());
            _writer.WriteElementString(nameof(Product.MainPictureId), entity.MainPictureId?.ToString() ?? string.Empty);
            _writer.WriteElementString(nameof(Product.Weight), ((decimal)product.Weight).ToString(_culture));
            _writer.WriteElementString(nameof(Product.Length), ((decimal)product.Length).ToString(_culture));
            _writer.WriteElementString(nameof(Product.Width), ((decimal)product.Width).ToString(_culture));
            _writer.WriteElementString(nameof(Product.Height), ((decimal)product.Height).ToString(_culture));
            _writer.WriteElementString(nameof(Product.AvailableStartDateTimeUtc), entity.AvailableStartDateTimeUtc?.ToString(_culture) ?? string.Empty);
            _writer.WriteElementString(nameof(Product.AvailableEndDateTimeUtc), entity.AvailableEndDateTimeUtc?.ToString(_culture) ?? string.Empty);
            _writer.WriteElementString(nameof(Product.BasePriceEnabled), ((bool)product.BasePriceEnabled).ToString());
            _writer.WriteElementString(nameof(Product.BasePriceMeasureUnit), (string)product.BasePriceMeasureUnit);
            _writer.WriteElementString(nameof(Product.BasePriceAmount), basePriceAmount?.ToString(_culture) ?? string.Empty);
            _writer.WriteElementString(nameof(Product.BasePriceBaseAmount), basePriceBaseAmount?.ToString() ?? string.Empty);
            _writer.WriteElementString(nameof(Product.BasePriceHasValue), ((bool)product.BasePriceHasValue).ToString());
            _writer.WriteElementString("BasePriceInfo", (string)product._BasePriceInfo);
            _writer.WriteElementString(nameof(Product.Visibility), ((int)entity.Visibility).ToString());
            _writer.WriteElementString(nameof(Product.Condition), ((int)entity.Condition).ToString());
            _writer.WriteElementString(nameof(Product.DisplayOrder), entity.DisplayOrder.ToString());
            _writer.WriteElementString(nameof(Product.IsSystemProduct), entity.IsSystemProduct.ToString());
            _writer.WriteElementString(nameof(Product.BundleTitleText), entity.BundleTitleText);
            _writer.WriteElementString(nameof(Product.BundlePerItemPricing), entity.BundlePerItemPricing.ToString());
            _writer.WriteElementString(nameof(Product.BundlePerItemShipping), entity.BundlePerItemShipping.ToString());
            _writer.WriteElementString(nameof(Product.BundlePerItemShoppingCart), entity.BundlePerItemShoppingCart.ToString());
            _writer.WriteElementString(nameof(Product.LowestAttributeCombinationPrice), lowestAttributeCombinationPrice?.ToString(_culture) ?? string.Empty);
            _writer.WriteElementString(nameof(Product.AttributeCombinationRequired), entity.AttributeCombinationRequired.ToString());
            _writer.WriteElementString(nameof(Product.AttributeChoiceBehaviour), ((int)entity.AttributeChoiceBehaviour).ToString());
            _writer.WriteElementString(nameof(Product.IsEsd), entity.IsEsd.ToString());
            _writer.WriteElementString(nameof(Product.CustomsTariffNumber), entity.CustomsTariffNumber);

            WriteLocalized(product);
            WritePriceLabel(product.ComparePriceLabel, nameof(Product.ComparePriceLabel));
            WriteDeliveryTime(product.DeliveryTime, nameof(Product.DeliveryTime));
            WriteQuantityUnit(product.QuantityUnit, nameof(Product.QuantityUnit));
            WriteCountry(product.CountryOfOrigin, nameof(Product.CountryOfOrigin));
            WriteAttributes(product);

            if (product.AppliedDiscounts != null)
            {
                _writer.WriteStartElement("AppliedDiscounts");
                foreach (dynamic discount in product.AppliedDiscounts)
                {
                    Discount entityDiscount = discount.Entity;

                    _writer.WriteStartElement("AppliedDiscount");
                    _writer.WriteElementString(nameof(Discount.Id), entityDiscount.Id.ToString());
                    _writer.WriteElementString(nameof(Discount.Name), (string)discount.Name);
                    _writer.WriteElementString(nameof(Discount.DiscountTypeId), entityDiscount.DiscountTypeId.ToString());
                    _writer.WriteElementString(nameof(Discount.UsePercentage), entityDiscount.UsePercentage.ToString());
                    _writer.WriteElementString(nameof(Discount.DiscountPercentage), entityDiscount.DiscountPercentage.ToString(_culture));
                    _writer.WriteElementString(nameof(Discount.DiscountAmount), entityDiscount.DiscountAmount.ToString(_culture));
                    _writer.WriteElementString(nameof(Discount.StartDateUtc), entityDiscount.StartDateUtc?.ToString(_culture) ?? string.Empty);
                    _writer.WriteElementString(nameof(Discount.EndDateUtc), entityDiscount.EndDateUtc?.ToString(_culture) ?? string.Empty);
                    _writer.WriteElementString(nameof(Discount.RequiresCouponCode), entityDiscount.RequiresCouponCode.ToString());
                    _writer.WriteElementString(nameof(Discount.CouponCode), entityDiscount.CouponCode);
                    _writer.WriteElementString(nameof(Discount.DiscountLimitationId), entityDiscount.DiscountLimitationId.ToString());
                    _writer.WriteElementString(nameof(Discount.LimitationTimes), entityDiscount.LimitationTimes.ToString());
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
                    _writer.WriteElementString(nameof(Download.Id), downloadEntity.Id.ToString());
                    _writer.WriteElementString(nameof(Download.DownloadGuid), downloadEntity.DownloadGuid.ToString());
                    _writer.WriteElementString(nameof(Download.UseDownloadUrl), downloadEntity.UseDownloadUrl.ToString());
                    _writer.WriteElementString(nameof(Download.DownloadUrl), downloadEntity.DownloadUrl);
                    _writer.WriteElementString(nameof(Download.IsTransient), downloadEntity.IsTransient.ToString());
                    _writer.WriteElementString(nameof(Download.UpdatedOnUtc), downloadEntity.UpdatedOnUtc.ToString(_culture));
                    _writer.WriteElementString(nameof(Download.EntityId), downloadEntity.EntityId.ToString());
                    _writer.WriteElementString(nameof(Download.EntityName), downloadEntity.EntityName);
                    _writer.WriteElementString(nameof(Download.FileVersion), downloadEntity.FileVersion);
                    _writer.WriteElementString(nameof(Download.Changelog), downloadEntity.Changelog);
                    if (!downloadEntity.UseDownloadUrl && mediaFile != null)
                    {
                        _writer.WriteElementString(nameof(MediaFile.MimeType), mediaFile.MimeType);
                        _writer.WriteElementString("Filename", mediaFile.Name);
                        _writer.WriteElementString(nameof(MediaFile.Extension), mediaFile.Extension);
                        _writer.WriteElementString(nameof(MediaFile.MediaStorageId), mediaFile.MediaStorageId?.ToString() ?? string.Empty);
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
                    _writer.WriteElementString(nameof(TierPrice.Id), entityTierPrice.Id.ToString());
                    _writer.WriteElementString(nameof(TierPrice.ProductId), entityTierPrice.ProductId.ToString());
                    _writer.WriteElementString(nameof(TierPrice.StoreId), entityTierPrice.StoreId.ToString());
                    _writer.WriteElementString(nameof(TierPrice.CustomerRoleId), entityTierPrice.CustomerRoleId?.ToString() ?? string.Empty);
                    _writer.WriteElementString(nameof(TierPrice.Quantity), entityTierPrice.Quantity.ToString());
                    _writer.WriteElementString(nameof(TierPrice.Price), entityTierPrice.Price.ToString(_culture));
                    _writer.WriteElementString(nameof(TierPrice.CalculationMethod), ((int)entityTierPrice.CalculationMethod).ToString());
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
                    _writer.WriteElementString(nameof(ProductTag.Id), ((int)tag.Id).ToString());
                    _writer.WriteElementString(nameof(ProductTag.Name), (string)tag.Name);
                    _writer.WriteElementString("SeName", (string)tag.SeName);
                    _writer.WriteElementString(nameof(ProductTag.Published), entityTag.Published.ToString());

                    WriteLocalized(tag);

                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            if (product.ProductMediaFiles != null)
            {
                _writer.WriteStartElement("ProductFiles");
                foreach (dynamic productFile in product.ProductMediaFiles)
                {
                    ProductMediaFile entityProductFile = productFile.Entity;

                    _writer.WriteStartElement("ProductFile");
                    _writer.WriteElementString(nameof(ProductMediaFile.Id), entityProductFile.Id.ToString());
                    _writer.WriteElementString(nameof(ProductMediaFile.DisplayOrder), entityProductFile.DisplayOrder.ToString());

                    WriteMediaFile(productFile.File, "File");
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
                    _writer.WriteElementString(nameof(ProductCategory.Id), entityProductCategory.Id.ToString());
                    _writer.WriteElementString(nameof(ProductCategory.DisplayOrder), entityProductCategory.DisplayOrder.ToString());
                    _writer.WriteElementString(nameof(ProductCategory.IsFeaturedProduct), entityProductCategory.IsFeaturedProduct.ToString());

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
                    _writer.WriteElementString(nameof(ProductManufacturer.Id), entityProductManu.Id.ToString());
                    _writer.WriteElementString(nameof(ProductManufacturer.DisplayOrder), entityProductManu.DisplayOrder.ToString());
                    _writer.WriteElementString(nameof(ProductManufacturer.IsFeaturedProduct), entityProductManu.IsFeaturedProduct.ToString());

                    WriteManufacturer(productManu.Manufacturer, "Manufacturer");
                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            if (product.RelatedProducts != null)
            {
                _writer.WriteStartElement("RelatedProducts");
                foreach (dynamic relatedProduct in product.RelatedProducts)
                {
                    RelatedProduct rpEntity = relatedProduct.Entity;

                    _writer.WriteStartElement("RelatedProduct");
                    _writer.WriteElementString(nameof(RelatedProduct.Id), rpEntity.Id.ToString());
                    _writer.WriteElementString(nameof(RelatedProduct.ProductId2), rpEntity.ProductId2.ToString());
                    _writer.WriteElementString(nameof(RelatedProduct.DisplayOrder), rpEntity.DisplayOrder.ToString());
                    _writer.WriteEndElement();
                }
                _writer.WriteEndElement();
            }

            if (product.CrossSellProducts != null)
            {
                _writer.WriteStartElement("CrossSellProducts");
                foreach (dynamic crossSellProduct in product.CrossSellProducts)
                {
                    CrossSellProduct cspEntity = crossSellProduct.Entity;

                    _writer.WriteStartElement("CrossSellProduct");
                    _writer.WriteElementString(nameof(CrossSellProduct.Id), cspEntity.Id.ToString());
                    _writer.WriteElementString(nameof(CrossSellProduct.ProductId2), cspEntity.ProductId2.ToString());
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
                    _writer.WriteElementString(nameof(ProductBundleItem.Id), entityPbi.Id.ToString());
                    _writer.WriteElementString(nameof(ProductBundleItem.ProductId), entityPbi.ProductId.ToString());
                    _writer.WriteElementString(nameof(ProductBundleItem.BundleProductId), entityPbi.BundleProductId.ToString());
                    _writer.WriteElementString(nameof(ProductBundleItem.Quantity), entityPbi.Quantity.ToString());
                    _writer.WriteElementString(nameof(ProductBundleItem.Discount), entityPbi.Discount?.ToString(_culture) ?? string.Empty);
                    _writer.WriteElementString(nameof(ProductBundleItem.DiscountPercentage), entityPbi.DiscountPercentage.ToString());
                    _writer.WriteElementString(nameof(ProductBundleItem.Name), (string)bundleItem.Name);
                    _writer.WriteElementString(nameof(ProductBundleItem.ShortDescription), (string)bundleItem.ShortDescription);
                    _writer.WriteElementString(nameof(ProductBundleItem.FilterAttributes), entityPbi.FilterAttributes.ToString());
                    _writer.WriteElementString(nameof(ProductBundleItem.HideThumbnail), entityPbi.HideThumbnail.ToString());
                    _writer.WriteElementString(nameof(ProductBundleItem.Visible), entityPbi.Visible.ToString());
                    _writer.WriteElementString(nameof(ProductBundleItem.Published), entityPbi.Published.ToString());
                    _writer.WriteElementString(nameof(ProductBundleItem.DisplayOrder), ((int)bundleItem.DisplayOrder).ToString());
                    _writer.WriteElementString(nameof(ProductBundleItem.CreatedOnUtc), entityPbi.CreatedOnUtc.ToString(_culture));
                    _writer.WriteElementString(nameof(ProductBundleItem.UpdatedOnUtc), entityPbi.UpdatedOnUtc.ToString(_culture));

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
                    _writer.WriteElementString(nameof(ProductVariantAttribute.Id), entityPva.Id.ToString());
                    _writer.WriteElementString(nameof(ProductVariantAttribute.TextPrompt), (string)pva.TextPrompt);
                    _writer.WriteElementString(nameof(ProductVariantAttribute.IsRequired), entityPva.IsRequired.ToString());
                    _writer.WriteElementString(nameof(ProductVariantAttribute.AttributeControlTypeId), entityPva.AttributeControlTypeId.ToString());
                    _writer.WriteElementString(nameof(ProductVariantAttribute.DisplayOrder), entityPva.DisplayOrder.ToString());

                    _writer.WriteStartElement("Attribute");
                    _writer.WriteElementString(nameof(ProductAttribute.Id), entityPa.Id.ToString());
                    _writer.WriteElementString(nameof(ProductAttribute.Alias), entityPa.Alias);
                    _writer.WriteElementString(nameof(ProductAttribute.Name), entityPa.Name);
                    _writer.WriteElementString(nameof(ProductAttribute.Description), entityPa.Description);
                    _writer.WriteElementString(nameof(ProductAttribute.AllowFiltering), entityPa.AllowFiltering.ToString());
                    _writer.WriteElementString(nameof(ProductAttribute.DisplayOrder), entityPa.DisplayOrder.ToString());
                    _writer.WriteElementString(nameof(ProductAttribute.FacetTemplateHint), ((int)entityPa.FacetTemplateHint).ToString());
                    _writer.WriteElementString(nameof(ProductAttribute.IndexOptionNames), entityPa.IndexOptionNames.ToString());

                    WriteLocalized(pva.Attribute);
                    _writer.WriteEndElement();  // Attribute

                    _writer.WriteStartElement("AttributeValues");
                    foreach (dynamic value in pva.Attribute.Values)
                    {
                        ProductVariantAttributeValue entityPvav = value.Entity;

                        _writer.WriteStartElement("AttributeValue");
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.Id), entityPvav.Id.ToString());
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.Alias), (string)value.Alias);
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.Name), (string)value.Name);
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.Color), (string)value.Color);
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.PriceAdjustment), ((decimal)value.PriceAdjustment).ToString(_culture));
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.WeightAdjustment), ((decimal)value.WeightAdjustment).ToString(_culture));
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.IsPreSelected), entityPvav.IsPreSelected.ToString());
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.DisplayOrder), entityPvav.DisplayOrder.ToString());
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.ValueTypeId), entityPvav.ValueTypeId.ToString());
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.LinkedProductId), entityPvav.LinkedProductId.ToString());
                        _writer.WriteElementString(nameof(ProductVariantAttributeValue.Quantity), entityPvav.Quantity.ToString());

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
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.Id), entityPvac.Id.ToString());
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.StockQuantity), entityPvac.StockQuantity.ToString());
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.AllowOutOfStockOrders), entityPvac.AllowOutOfStockOrders.ToString());
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.RawAttributes), entityPvac.RawAttributes);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.Sku), entityPvac.Sku);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.Gtin), entityPvac.Gtin);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.ManufacturerPartNumber), entityPvac.ManufacturerPartNumber);

                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.Price), entityPvac.Price?.ToString(_culture) ?? string.Empty);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.Price), entityPvac.Price?.ToString(_culture) ?? string.Empty);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.Length), entityPvac.Length?.ToString(_culture) ?? string.Empty);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.Width), entityPvac.Width?.ToString(_culture) ?? string.Empty);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.Height), entityPvac.Height?.ToString(_culture) ?? string.Empty);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.BasePriceAmount), entityPvac.BasePriceAmount?.ToString(_culture) ?? string.Empty);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.BasePriceBaseAmount), entityPvac.BasePriceBaseAmount?.ToString() ?? string.Empty);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.AssignedMediaFileIds), entityPvac.AssignedMediaFileIds);
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.IsActive), entityPvac.IsActive.ToString());
                    _writer.WriteElementString(nameof(ProductVariantAttributeCombination.HashCode), entityPvac.HashCode.ToString());

                    WriteDeliveryTime(combination.DeliveryTime, nameof(ProductVariantAttributeCombination.DeliveryTime));
                    WriteQuantityUnit(combination.QuantityUnit, nameof(ProductVariantAttributeCombination.QuantityUnit));

                    _writer.WriteStartElement("Files");
                    foreach (dynamic assignedFile in combination.Files)
                    {
                        WriteMediaFile(assignedFile, "File");
                    }
                    _writer.WriteEndElement();  // Files
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
                    _writer.WriteElementString(nameof(ProductSpecificationAttribute.Id), entityPsa.Id.ToString());
                    _writer.WriteElementString(nameof(ProductSpecificationAttribute.ProductId), entityPsa.ProductId.ToString());
                    _writer.WriteElementString(nameof(ProductSpecificationAttribute.SpecificationAttributeOptionId), entityPsa.SpecificationAttributeOptionId.ToString());
                    _writer.WriteElementString(nameof(ProductSpecificationAttribute.AllowFiltering), entityPsa.AllowFiltering.ToString());
                    _writer.WriteElementString(nameof(ProductSpecificationAttribute.ShowOnProductPage), entityPsa.ShowOnProductPage.ToString());
                    _writer.WriteElementString(nameof(ProductSpecificationAttribute.DisplayOrder), entityPsa.DisplayOrder.ToString());

                    dynamic option = psa.SpecificationAttributeOption;
                    SpecificationAttributeOption entitySao = option.Entity;
                    SpecificationAttribute entitySa = option.SpecificationAttribute.Entity;

                    _writer.WriteStartElement("SpecificationAttributeOption");
                    _writer.WriteElementString(nameof(SpecificationAttributeOption.Id), entitySao.Id.ToString());
                    _writer.WriteElementString(nameof(SpecificationAttributeOption.SpecificationAttributeId), entitySao.SpecificationAttributeId.ToString());
                    _writer.WriteElementString(nameof(SpecificationAttributeOption.DisplayOrder), entitySao.DisplayOrder.ToString());
                    _writer.WriteElementString(nameof(SpecificationAttributeOption.NumberValue), ((decimal)option.NumberValue).ToString(_culture));
                    _writer.WriteElementString(nameof(SpecificationAttributeOption.Color), (string)option.Color);
                    _writer.WriteElementString(nameof(SpecificationAttributeOption.Name), (string)option.Name);
                    _writer.WriteElementString(nameof(SpecificationAttributeOption.Alias), (string)option.Alias);

                    WriteLocalized(option);

                    _writer.WriteStartElement("SpecificationAttribute");
                    _writer.WriteElementString(nameof(SpecificationAttribute.Id), entitySa.Id.ToString());
                    _writer.WriteElementString(nameof(SpecificationAttribute.Name), (string)option.SpecificationAttribute.Name);
                    _writer.WriteElementString(nameof(SpecificationAttribute.Alias), (string)option.SpecificationAttribute.Alias);
                    _writer.WriteElementString(nameof(SpecificationAttribute.Essential), entitySa.Essential.ToString());
                    _writer.WriteElementString(nameof(SpecificationAttribute.DisplayOrder), entitySa.DisplayOrder.ToString());
                    _writer.WriteElementString(nameof(SpecificationAttribute.AllowFiltering), entitySa.AllowFiltering.ToString());
                    _writer.WriteElementString(nameof(SpecificationAttribute.ShowOnProductPage), entitySa.ShowOnProductPage.ToString());
                    _writer.WriteElementString(nameof(SpecificationAttribute.FacetSorting), ((int)entitySa.FacetSorting).ToString());
                    _writer.WriteElementString(nameof(SpecificationAttribute.FacetTemplateHint), ((int)entitySa.FacetTemplateHint).ToString());
                    _writer.WriteElementString(nameof(SpecificationAttribute.IndexOptionNames), entitySa.IndexOptionNames.ToString());

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
