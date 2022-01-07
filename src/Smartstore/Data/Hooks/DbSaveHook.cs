using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data.Hooks
{
    public abstract class DbSaveHook<TContext, TEntity> : IDbSaveHook<TContext>
        where TContext : DbContext
        where TEntity : class
    {
        #region IDbSaveHook<TContext> interface (explicit)

        Task<HookResult> IDbSaveHook.OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            return Task.FromResult(OnBeforeSave(entry));
        }

        Task IDbSaveHook.OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            OnBeforeSaveCompleted(entries);
            return Task.CompletedTask;
        }

        Task<HookResult> IDbSaveHook.OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            return Task.FromResult(OnAfterSave(entry));
        }

        Task IDbSaveHook.OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            OnAfterSaveCompleted(entries);
            return Task.CompletedTask;
        }

        #endregion

        #region Sync protected methods

        protected virtual HookResult OnBeforeSave(IHookedEntity entry)
        {
            var entity = entry.Entity as TEntity;
            switch (entry.InitialState)
            {
                case EntityState.Added:
                    return OnInserting(entity, entry);
                case EntityState.Modified:
                    return OnUpdating(entity, entry);
                case EntityState.Deleted:
                    return OnDeleting(entity, entry);
                default:
                    return HookResult.Void;
            }
        }

        protected virtual HookResult OnInserting(TEntity entity, IHookedEntity entry) => HookResult.Void;
        protected virtual HookResult OnUpdating(TEntity entity, IHookedEntity entry) => HookResult.Void;
        protected virtual HookResult OnDeleting(TEntity entity, IHookedEntity entry) => HookResult.Void;

        protected virtual void OnBeforeSaveCompleted(IEnumerable<IHookedEntity> entries)
        {
        }

        public virtual HookResult OnAfterSave(IHookedEntity entry)
        {
            var entity = entry.Entity as TEntity;
            switch (entry.InitialState)
            {
                case EntityState.Added:
                    return OnInserted(entity, entry);
                case EntityState.Modified:
                    return OnUpdated(entity, entry);
                case EntityState.Deleted:
                    return OnDeleted(entity, entry);
                default:
                    return HookResult.Void;
            }
        }

        protected virtual HookResult OnInserted(TEntity entity, IHookedEntity entry) => HookResult.Void;
        protected virtual HookResult OnUpdated(TEntity entity, IHookedEntity entry) => HookResult.Void;
        protected virtual HookResult OnDeleted(TEntity entity, IHookedEntity entry) => HookResult.Void;

        public virtual void OnAfterSaveCompleted(IEnumerable<IHookedEntity> entries)
        {
        }

        #endregion
    }
}
