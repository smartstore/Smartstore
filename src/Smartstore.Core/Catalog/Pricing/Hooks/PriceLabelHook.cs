using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Pricing
{
    internal class PriceLabelHook : AsyncDbSaveHook<PriceLabel>
    {
        private readonly SmartDbContext _db;
        private readonly PriceSettings _priceSettings;
        private string _hookErrorMessage;

        public PriceLabelHook(SmartDbContext db, PriceSettings priceSettings)
        {
            _db = db;
            _priceSettings = priceSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override Task<HookResult> OnDeletingAsync(PriceLabel entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.Id == _priceSettings.DefaultRegularPriceLabelId)
            {
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.PriceLabel.CantDeleteDefaultRegularPriceLabel");
            }
            else if (entity.Id == _priceSettings.DefaultComparePriceLabelId)
            {
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.PriceLabel.CantDeleteDefaultComparePriceLabel");
            }

            return Task.FromResult(HookResult.Ok);
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new HookException(message);
            }

            return Task.CompletedTask;
        }
    }
}
