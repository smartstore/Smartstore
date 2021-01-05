using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Tests.Data.Hooks
{
    internal class Hook_Entity_Inserted_Deleted_Update : DbSaveHook<SmartDbContext, BaseEntity>
    {
        protected override HookResult OnInserted(BaseEntity entity, IHookedEntity entry) => HookResult.Ok;
        protected override HookResult OnDeleted(BaseEntity entity, IHookedEntity entry) => HookResult.Ok;
        protected override HookResult OnUpdating(BaseEntity entity, IHookedEntity entry) => HookResult.Ok;
        protected override HookResult OnUpdated(BaseEntity entity, IHookedEntity entry) => HookResult.Ok;
    }

    internal class Hook_Acl_Deleted : DbSaveHook<SmartDbContext, IAclRestricted>
    {
        protected override HookResult OnDeleted(IAclRestricted entity, IHookedEntity entry) => HookResult.Ok;
    }

    [Important]
    internal class Hook_Auditable_Inserting_Updating_Important : DbSaveHook<SmartDbContext, IAuditable>
    {
        protected override HookResult OnInserting(IAuditable entity, IHookedEntity entry) => HookResult.Ok;
        protected override HookResult OnUpdating(IAuditable entity, IHookedEntity entry) => HookResult.Ok;
    }

    internal class Hook_SoftDeletable_Updating_ChangingState : DbSaveHook<SmartDbContext, ISoftDeletable>
    {
        protected override HookResult OnUpdating(ISoftDeletable entity, IHookedEntity entry)
        {
            entry.State = EntityState.Unchanged;
            return HookResult.Ok;
        }
    }

    internal class Hook_LocalizedEntity_Deleted : DbSaveHook<SmartDbContext, ILocalizedEntity>
    {
        protected override HookResult OnDeleted(ILocalizedEntity entity, IHookedEntity entry) => HookResult.Ok;
    }

    internal class Hook_Product_Post : IDbSaveHook
    {
        public Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
            //return Task.FromResult(HookResult.Void);
        }

        public Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.EntityType != typeof(Product))
                return Task.FromResult(HookResult.Void);

            return Task.FromResult(HookResult.Ok);
        }

        public Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        public Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }
    }

    internal class Hook_Category_Pre : IDbSaveHook
    {
        public Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.EntityType != typeof(Category))
                return Task.FromResult(HookResult.Void);

            return Task.FromResult(HookResult.Ok);
        }

        public Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        public Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        public Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }
    }
}