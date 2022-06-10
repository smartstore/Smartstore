using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data.Hooks
{
    public abstract class AsyncDbSaveHook<TContext, TEntity> : IDbSaveHook<TContext>
        where TContext : DbContext
        where TEntity : class
    {
        #region IDbSaveHook<TContext> interface

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public virtual Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        #endregion

        /// <summary>
        /// Called before a new entity is about to be inserted.
        /// </summary>
        protected virtual Task<HookResult> OnInsertingAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);

        /// <summary>
        /// Called before an existing entity is about to be updated.
        /// </summary>
        protected virtual Task<HookResult> OnUpdatingAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);

        /// <summary>
        /// Called before an existing entity is about to be deleted.
        /// </summary>
        protected virtual Task<HookResult> OnDeletingAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);

        /// <summary>
        /// Called after a new entity was inserted.
        /// </summary>
        protected virtual Task<HookResult> OnInsertedAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);

        /// <summary>
        /// Called after an existing entity was updated.
        /// </summary>
        protected virtual Task<HookResult> OnUpdatedAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);

        /// <summary>
        /// Called after an existing entity was deleted.
        /// </summary>
        protected virtual Task<HookResult> OnDeletedAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Void);
    }
}
