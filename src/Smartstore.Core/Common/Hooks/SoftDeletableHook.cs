using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Common.Hooks
{
    [Important, Order(int.MinValue)]
    [ServiceLifetime(ServiceLifetime.Singleton)]
    internal class SoftDeletableHook : AsyncDbSaveHook<ISoftDeletable>
    {
        private readonly List<IHookedEntity> _softDeletedEntries = new();

        protected override Task<HookResult> OnDeletingAsync(ISoftDeletable entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Suppress physical deletion of a soft deletable entity. Set "Deleted" property to True instead.

            if (entity.ForceDeletion)
            {
                // ...but not if "ForceDeletion" is true.
                return Task.FromResult(HookResult.Ok);
            }

            entry.State = Smartstore.Data.EntityState.Modified;
            entity.Deleted = true;

            _softDeletedEntries.Add(entry);

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatingAsync(ISoftDeletable entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // TODO: (core) Handle IAclRestricted, ISlugSupported, Product, Manufacturer and Category in their specific hooks, not here. See Smartstore.Core.Catalog.Products.ProductHook.

            if (entry.IsSoftDeleted == true)
            {
                _softDeletedEntries.Add(entry);
            }

            return Task.FromResult(HookResult.Ok);
        }

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Sort of "freeze" the soft deletion state, because POST save hooks cannot reflect over prop changes anymore.
            _softDeletedEntries.Each(x => x.IsSoftDeleted = true);
            _softDeletedEntries.Clear();

            return Task.CompletedTask;
        }
    }
}