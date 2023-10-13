using Humanizer;
using Humanizer.Localisation;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    public class PriceLabelService : IPriceLabelService
    {
        private readonly SmartDbContext _db;
        private readonly PriceSettings _priceSettings;

        private Dictionary<int, PriceLabel> _allPriceLabels;
        private PriceLabel _defaultComparePriceLabel;
        private PriceLabel _defaultRegularPriceLabel;

        public PriceLabelService(SmartDbContext db, PriceSettings priceSettings)
        {
            _db = db;
            _priceSettings = priceSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        
        public virtual PriceLabel GetDefaultComparePriceLabel()
        {
            return _defaultComparePriceLabel ??= GetPriceLabel(_priceSettings.DefaultComparePriceLabelId, true);
        }

        public virtual PriceLabel GetDefaultRegularPriceLabel()
        {
            return _defaultRegularPriceLabel ??= GetPriceLabel(_priceSettings.DefaultRegularPriceLabelId, false);
        }

        private PriceLabel GetPriceLabel(int? id, bool forComparePrice)
        {
            var allLabels = GetAllPriceLabels();

            if (id > 0 && allLabels.TryGetValue(id.Value, out var label))
            {
                return label;
            }

            label = forComparePrice
                ? allLabels.Values.FirstOrDefault(x => x.IsRetailPrice)
                : allLabels.Values.FirstOrDefault(x => !x.IsRetailPrice);

            return label ?? allLabels.Values.FirstOrDefault();
        }

        private Dictionary<int, PriceLabel> GetAllPriceLabels()
        {
            if (_allPriceLabels == null)
            {
                _allPriceLabels = _db.PriceLabels
                    .AsNoTracking()
                    .OrderBy(x => x.DisplayOrder)
                    .ToDictionary(x => x.Id);
            }

            return _allPriceLabels;
        }

        public virtual PriceLabel GetComparePriceLabel(Product product)
        {
            Guard.NotNull(product, nameof(product));

            var labelId = product.ComparePriceLabelId.GetValueOrDefault();
            if (labelId == 0)
            {
                return GetDefaultComparePriceLabel();
            }

            if (_db.IsReferenceLoaded(product, x => x.ComparePriceLabel, out var entry))
            {
                return entry.CurrentValue;
            }

            return GetPriceLabel(labelId, true);
        }

        public virtual PriceLabel GetRegularPriceLabel(Product product)
        {
            return GetDefaultRegularPriceLabel();
        }

        public virtual (LocalizedValue<string>, string) GetPricePromoBadge(CalculatedPrice price)
        {
            Guard.NotNull(price, nameof(price));
            
            if (!price.Saving.HasSaving || price.RegularPrice == null)
            {
                return (null, null);
            }

            var validUntilUtc = price.ValidUntilUtc;
            var isLimitedOffer = validUntilUtc.HasValue && validUntilUtc > DateTime.UtcNow;
            var badgeStyle = isLimitedOffer ? _priceSettings.LimitedOfferBadgeStyle : _priceSettings.OfferBadgeStyle;

            LocalizedValue<string> badgeLabel;

            foreach (var discount in price.AppliedDiscounts)
            {
                badgeLabel = discount.GetLocalized(x => x.OfferBadgeLabel);
                if (badgeLabel.HasValue())
                {
                    return (badgeLabel, badgeStyle);
                }
            }

            badgeLabel = isLimitedOffer
                ? _priceSettings.GetLocalizedSetting(x => x.LimitedOfferBadgeLabel)
                : _priceSettings.GetLocalizedSetting(x => x.OfferBadgeLabel);

            if (isLimitedOffer && !badgeLabel.HasValue())
            {
                // If LimitedOfferBadgeLabel is empty, fall back to OfferBadgeLabel.
                badgeLabel = _priceSettings.GetLocalizedSetting(x => x.OfferBadgeLabel);
            }

            return (badgeLabel, badgeStyle);
        }

        public virtual LocalizedString GetPromoCountdownText(CalculatedPrice price)
        {
            Guard.NotNull(price);

            if (price.ValidUntilUtc == null)
            {
                return null;
            }

            var threshold = _priceSettings.ShowOfferCountdownRemainingHours;

            foreach (var discount in price.AppliedDiscounts)
            {
                if (discount.ShowCountdownRemainingHours.HasValue)
                {
                    threshold = discount.ShowCountdownRemainingHours;
                    break;
                }
            }

            if (threshold.GetValueOrDefault() < 1)
            {
                return null;
            }

            var maxTime = TimeSpan.FromHours(threshold.Value);
            var remainingTime = price.ValidUntilUtc.Value - DateTime.UtcNow;

            if (remainingTime > maxTime)
            {
                return null;
            }

            var humanizedTimeString = remainingTime.Humanize(precision: 2, maxUnit: TimeUnit.Day, minUnit: TimeUnit.Minute);
            return T("Products.Price.OfferCountdown", humanizedTimeString);
        }
    }
}
