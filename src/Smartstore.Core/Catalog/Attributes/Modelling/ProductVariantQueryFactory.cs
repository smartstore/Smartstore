using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Utilities;

namespace Smartstore.Core.Catalog.Attributes.Modelling
{
    public partial class ProductVariantQueryFactory : IProductVariantQueryFactory
    {
        internal static readonly Regex IsVariantKey = IsVariantKeyRegex();
        internal static readonly Regex IsVariantAliasKey = IsVariantAliasKeyRegex();
        internal static readonly Regex IsGiftCardKey = IsGiftCardKeyRegex();
        internal static readonly Regex IsCheckoutAttributeKey = IsCheckoutAttributeKeyRegex();

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWorkContext _workContext;
        private readonly ICatalogSearchQueryAliasMapper _catalogSearchQueryAliasMapper;
        private Multimap<string, string> _queryItems;

        public ProductVariantQueryFactory(
            IHttpContextAccessor httpContextAccessor,
            IWorkContext workContext,
            ICatalogSearchQueryAliasMapper catalogSearchQueryAliasMapper)
        {
            _httpContextAccessor = httpContextAccessor;
            _workContext = workContext;
            _catalogSearchQueryAliasMapper = catalogSearchQueryAliasMapper;
        }

        protected Multimap<string, string> QueryItems
        {
            get
            {
                if (_queryItems == null)
                {
                    _queryItems = new Multimap<string, string>();

                    var request = _httpContextAccessor?.HttpContext?.Request;
                    if (request != null)
                    {
                        if (request.HasFormContentType)
                        {
                            request.Form?.Keys
                                .Where(x => x.HasValue())
                                .Select(x => new { key = x, val = request.Form[x] })
                                .Each(x => _queryItems.AddRange(x.key, x.val.SelectMany(y => y.SplitSafe(','))));
                        }

                        request.Query?.Keys
                            .Where(x => x.HasValue())
                            .Select(x => new { key = x, val = request.Query[x] })
                            .Each(x => _queryItems.AddRange(x.key, x.val.SelectMany(y => y.SplitSafe(','))));
                    }
                }

                return _queryItems;
            }
        }

        public ProductVariantQuery Current { get; private set; }

        public ProductVariantQuery CreateFromQuery()
        {
            var query = new ProductVariantQuery();
            Current = query;

            var request = _httpContextAccessor?.HttpContext?.Request;
            if (request == null)
            {
                return query;
            }

            var languageId = _workContext.WorkingLanguage.Id;

            foreach (var item in QueryItems)
            {
                if (!item.Value.Any())
                {
                    continue;
                }

                if (IsVariantKey.IsMatch(item.Key))
                {
                    ConvertVariant(query, item.Key, item.Value);
                }
                else if (IsGiftCardKey.IsMatch(item.Key))
                {
                    item.Value.Each(value => ConvertGiftCard(query, item.Key, value));
                }
                else if (IsCheckoutAttributeKey.IsMatch(item.Key))
                {
                    ConvertCheckoutAttribute(query, item.Key, item.Value);
                }
                else if (IsVariantAliasKey.IsMatch(item.Key))
                {
                    ConvertVariantAlias(query, item.Key, item.Value, languageId);
                }
                else if (item.Key.EqualsNoCase("pvari") &&
                    int.TryParse(item.Value.FirstOrDefault()?.NullEmpty() ?? "0", out var variantCombinationId) &&
                    variantCombinationId != 0)
                {
                    query.VariantCombinationId = variantCombinationId;
                }
                else
                {
                    ConvertItems(request, query, item.Key, item.Value);
                }
            }

            return query;
        }

        private DateTime? ConvertToDate(string key, string value)
        {
            var year = 0;
            var month = 0;
            var day = 0;

            if (key.EndsWith("-date"))
            {
                // Convert from one query string item.
                var dateItems = value.SplitSafe('-').ToArray();
                year = dateItems.ElementAtOrDefault(0).ToInt();
                month = dateItems.ElementAtOrDefault(1).ToInt();
                day = dateItems.ElementAtOrDefault(2).ToInt();
            }

            if (year > 0 && month > 0 && day > 0)
            {
                return CommonHelper.TryAction(() => new DateTime(year, month, day));
            }

            return null;
        }

        protected virtual void ConvertVariant(ProductVariantQuery query, string key, ICollection<string> values)
        {
            var ids = key.Replace("pvari", string.Empty).SplitSafe('-').ToArray();
            if (ids.Length < 4)
            {
                return;
            }

            var isDate = key.EndsWith("-date");
            var isFile = key.EndsWith("-file");
            var isText = key.EndsWith("-text");
            var isTextArea = key.EndsWith("-textarea");

            if (isDate || isFile || isText || isTextArea)
            {
                var value = GetValue(values, isText, isTextArea);
                var variant = new ProductVariantQueryItem
                {
                    Value = value,
                    ProductId = ids[0].ToInt(),
                    BundleItemId = ids[1].ToInt(),
                    AttributeId = ids[2].ToInt(),
                    VariantAttributeId = ids[3].ToInt(),
                    IsFile = isFile,
                    IsText = isText,
                    IsTextArea = isTextArea
                };

                if (isDate)
                {
                    variant.Date = ConvertToDate(key, value);
                }

                query.AddVariant(variant);
            }
            else
            {
                foreach (var value in values)
                {
                    query.AddVariant(new()
                    {
                        Value = value ?? string.Empty,
                        ProductId = ids[0].ToInt(),
                        BundleItemId = ids[1].ToInt(),
                        AttributeId = ids[2].ToInt(),
                        VariantAttributeId = ids[3].ToInt()
                    });
                }
            }
        }

        protected virtual void ConvertVariantAlias(ProductVariantQuery query, string key, ICollection<string> values, int languageId)
        {
            var ids = key.SplitSafe('-').ToArray();
            var len = ids.Length;
            if (len < 4)
            {
                return;
            }

            var isDate = key.EndsWith("-date");
            var isFile = key.EndsWith("-file");
            var isText = key.EndsWith("-text");
            var isTextArea = key.EndsWith("-textarea");

            if (isDate || isFile || isText || isTextArea)
            {
                ids = ids.Take(len - 1).ToArray();
                len = ids.Length;
            }

            var alias = string.Join('-', ids.Take(len - 3));
            var attributeId = _catalogSearchQueryAliasMapper.GetVariantIdByAlias(alias, languageId);
            if (attributeId == 0)
            {
                return;
            }

            var productId = ids.ElementAtOrDefault(len - 3).ToInt();
            var bundleItemId = ids.ElementAtOrDefault(len - 2).ToInt();
            var variantAttributeId = ids.ElementAtOrDefault(len - 1).ToInt();

            if (productId == 0 || variantAttributeId == 0)
            {
                return;
            }

            if (isDate || isFile || isText || isTextArea)
            {
                var value = GetValue(values, isText, isTextArea);
                var variant = new ProductVariantQueryItem
                {
                    Value = value,
                    ProductId = productId,
                    BundleItemId = bundleItemId,
                    AttributeId = attributeId,
                    VariantAttributeId = variantAttributeId,
                    Alias = alias,
                    IsFile = isFile,
                    IsText = isText,
                    IsTextArea = isTextArea,
                };

                if (isDate)
                {
                    variant.Date = ConvertToDate(key, value);
                }

                query.AddVariant(variant);
            }
            else
            {
                foreach (var value in values)
                {
                    // We cannot use GetVariantOptionIdByAlias. It doesn't necessarily provide a ProductVariantAttributeValue.Id associated with this product.
                    //var optionId = _catalogSearchQueryAliasMapper.GetVariantOptionIdByAlias(value, attributeId, languageId);
                    var optionId = 0;
                    string valueAlias = null;

                    var valueIds = value.SplitSafe('-').ToArray();
                    if (valueIds.Length >= 2)
                    {
                        optionId = valueIds.ElementAtOrDefault(valueIds.Length - 1).ToInt();
                        valueAlias = string.Join('-', valueIds.Take(valueIds.Length - 1));
                    }

                    query.AddVariant(new()
                    {
                        Value = optionId == 0 ? value.EmptyNull() : optionId.ToString(),
                        ProductId = productId,
                        BundleItemId = bundleItemId,
                        AttributeId = attributeId,
                        VariantAttributeId = variantAttributeId,
                        Alias = alias,
                        ValueAlias = optionId != 0 ? valueAlias : null
                    });
                }
            }
        }

        protected virtual void ConvertGiftCard(ProductVariantQuery query, string key, string value)
        {
            var elements = key.Replace("giftcard", string.Empty).SplitSafe('-').ToArray();
            if (elements.Length > 2)
            {
                var giftCard = new GiftCardQueryItem(elements[2], value)
                {
                    ProductId = elements[0].ToInt(),
                    BundleItemId = elements[1].ToInt()
                };

                query.AddGiftCard(giftCard);
            }
        }

        protected virtual void ConvertCheckoutAttribute(ProductVariantQuery query, string key, ICollection<string> values)
        {
            var ids = key.Replace("cattr", string.Empty).SplitSafe('-').ToArray();
            if (ids.Length <= 0)
            {
                return;
            }

            var attributeId = ids[0].ToInt();
            var isDate = key.EndsWith("-date");
            var isFile = key.EndsWith("-file");
            var isText = key.EndsWith("-text");
            var isTextArea = key.EndsWith("-textarea");

            if (isDate || isFile || isText || isTextArea)
            {
                var value = GetValue(values, isText, isTextArea);
                var attribute = new CheckoutAttributeQueryItem
                {
                    Value = value,
                    AttributeId = attributeId,
                    IsFile = isFile,
                    IsText = isText,
                    IsTextArea = isTextArea
                };

                if (isDate)
                {
                    attribute.Date = ConvertToDate(key, value);
                }

                query.AddCheckoutAttribute(attribute);
            }
            else
            {
                foreach (var value in values)
                {
                    query.AddCheckoutAttribute(new()
                    {
                        Value = value.EmptyNull(),
                        AttributeId = attributeId
                    });
                }
            }
        }

        protected virtual void ConvertItems(HttpRequest request, ProductVariantQuery query, string key, ICollection<string> values)
        {
        }

        protected virtual string GetValue(ICollection<string> values, bool isText, bool isTextArea)
        {
            if (isText)
            {
                return string.Join(',', values.Select(x => TextRegex().Replace(x, " ")));
            }
            else if (isTextArea)
            {
                return string.Join(',', values.Select(x => TextAreaRegex().Replace(x, " ")));
            }

            return values.First().EmptyNull();
        }

        #region Regex

        [GeneratedRegex("pvari[0-9]+-[0-9]+-[0-9]+-[0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "de-DE")]
        private static partial Regex IsVariantKeyRegex();

        [GeneratedRegex("\\w+-[0-9]+-[0-9]+-[0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "de-DE")]
        private static partial Regex IsVariantAliasKeyRegex();

        [GeneratedRegex("giftcard[0-9]+-[0-9]+-\\.\\w+$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "de-DE")]
        private static partial Regex IsGiftCardKeyRegex();

        [GeneratedRegex("cattr[0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "de-DE")]
        private static partial Regex IsCheckoutAttributeKeyRegex();

        /// <summary>
        /// Matches non-printable and control characters.
        /// </summary>
        [GeneratedRegex(@"\p{C}+", RegexOptions.None)]
        private static partial Regex TextRegex();

        /// <summary>
        /// Matches non-printable and control characters except new line.
        /// </summary>
        [GeneratedRegex(@"[^\P{C}\n]+", RegexOptions.None)]
        private static partial Regex TextAreaRegex();

        #endregion
    }
}
