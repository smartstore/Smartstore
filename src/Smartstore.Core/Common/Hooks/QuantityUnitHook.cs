using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Hooks
{
    [Important]
    internal class QuantityUnitHook : AsyncDbSaveHook<QuantityUnit>
    {
        private readonly SmartDbContext _db;
        private string _hookErrorMessage;

        public QuantityUnitHook(SmartDbContext db)
        {
            _db = db;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// Sets all quantity units to <see cref="QuantityUnit.IsDefault"/> = false if the currently updated entity is the default quantity unit.
        /// </summary>
        protected override async Task<HookResult> OnInsertingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await ResetDefaultQuantityUnits(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Sets all quantity units to <see cref="QuantityUnit.IsDefault"/> = false if the currently updated entity is the default quantity unit.
        /// </summary>
        protected override async Task<HookResult> OnUpdatingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await ResetDefaultQuantityUnits(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Prevents saving of quantity unit if it's referenced by products or attribute combinations.
        /// </summary>
        protected override async Task<HookResult> OnDeletingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.IsDefault == true)
            {
                // Cannot delete the default quantity unit.
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.QuantityUnit.CannotDeleteDefaultQuantityUnit");
            }
            else if (await _db.Products.AnyAsync(x => x.QuantityUnitId == entity.Id || x.ProductVariantAttributeCombinations.Any(c => c.QuantityUnitId == entity.Id), cancelToken))
            {
                // Cannot delete if there are associations to active products or attribute combinations.
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.QuantityUnit.CannotDeleteAssignedProducts");
            }

            return HookResult.Ok;
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new SmartException(message);
            }

            return Task.CompletedTask;
        }

        private async Task ResetDefaultQuantityUnits(QuantityUnit entity, CancellationToken cancelToken)
        {
            Guard.NotNull(entity, nameof(entity));

            if (entity.IsDefault)
            {
                var quantityUnits = await _db.QuantityUnits
                    .Where(x => x.IsDefault && x.Id != entity.Id)
                    .ToListAsync(cancelToken);

                if (quantityUnits.Any())
                {
                    quantityUnits.Each(x => x.IsDefault = false);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }
        }
    }
}