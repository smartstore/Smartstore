using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data.Hooks
{
    public abstract class AsyncDbSaveHook<TContext, TEntity> : IDbSaveHook<TContext>
        where TContext : DbContext
        where TEntity : class
    {
        #region IDbSaveHook<TContext> interface

        public virtual async Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var entity = entry.Entity as TEntity;
            switch (entry.InitialState)
            {
                case EntityState.Added:
                    return await OnInsertingAsync(entity, entry, cancelToken);
                case EntityState.Modified:
                    return await OnUpdatingAsync(entity, entry, cancelToken);
                case EntityState.Deleted:
                    return await OnDeletingAsync(entity, entry, cancelToken);
                default:
                    return HookResult.Void;
            }
        }

        public virtual async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var entity = entry.Entity as TEntity;
            switch (entry.InitialState)
            {
                case EntityState.Added:
                    return await OnInsertedAsync(entity, entry, cancelToken);
                case EntityState.Modified:
                    return await OnUpdatedAsync(entity, entry, cancelToken);
                case EntityState.Deleted:
                    return await OnDeletedAsync(entity, entry, cancelToken);
                default:
                    return HookResult.Void;
            }
        }

        public virtual Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        #endregion

        protected virtual Task<HookResult> OnInsertingAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);
        protected virtual Task<HookResult> OnUpdatingAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);
        protected virtual Task<HookResult> OnDeletingAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);

        protected virtual Task<HookResult> OnInsertedAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);
        protected virtual Task<HookResult> OnUpdatedAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);
        protected virtual Task<HookResult> OnDeletedAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);
    }
}
