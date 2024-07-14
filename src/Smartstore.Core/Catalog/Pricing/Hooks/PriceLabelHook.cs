﻿using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Pricing
{
    internal class PriceLabelHook(SmartDbContext db, PriceSettings priceSettings) : AsyncDbSaveHook<PriceLabel>
    {
        private readonly SmartDbContext _db = db;
        private readonly PriceSettings _priceSettings = priceSettings;
        private string _hookErrorMessage;

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
