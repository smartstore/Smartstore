using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    internal class SpecificationAttributeHook : AsyncDbSaveHook<SpecificationAttribute>
    {
        private readonly SmartDbContext _db;
        private readonly HashSet<int> _deletedAttributeOptionIds = new();

        public SpecificationAttributeHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletingAsync(SpecificationAttribute entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            _deletedAttributeOptionIds.AddRange(entity.SpecificationAttributeOptions.Select(x => x.Id));

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnDeletedAsync(SpecificationAttribute entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Delete localized properties of attribute values that were deleted by referential integrity.
            if (_deletedAttributeOptionIds.Any())
            {
                await _db.LocalizedProperties
                    .Where(x => _deletedAttributeOptionIds.Contains(x.EntityId) && x.LocaleKeyGroup == nameof(SpecificationAttributeOption))
                    .ExecuteDeleteAsync(cancelToken);

                _deletedAttributeOptionIds.Clear();
            }
        }
    }
}
