using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data.Hooks
{
    public abstract class DbSaveHook<TContext, TEntity> : IDbSaveHook<TContext>
        where TContext : DbContext
        where TEntity : class
    {
        #region IDbSaveHook<TContext> interface (explicit)

        Task IDbSaveHook.OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            OnBeforeSave(entry);
            return Task.CompletedTask;
        }

        Task IDbSaveHook.OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            OnBeforeSaveCompleted(entries);
            return Task.CompletedTask;
        }

        Task IDbSaveHook.OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            OnAfterSave(entry);
            return Task.CompletedTask;
        }

        Task IDbSaveHook.OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            OnAfterSaveCompleted(entries);
            return Task.CompletedTask;
        }

        #endregion

        #region Sync protected methods

        protected virtual void OnBeforeSave(IHookedEntity entry)
        {
            var entity = entry.Entity as TEntity;
            switch (entry.InitialState)
            {
                case EntityState.Added:
                    OnInserting(entity, entry);
                    break;
                case EntityState.Modified:
                    OnUpdating(entity, entry);
                    break;
                case EntityState.Deleted:
                    OnDeleting(entity, entry);
                    break;
            }
        }

        protected virtual void OnInserting(TEntity entity, IHookedEntity entry)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnUpdating(TEntity entity, IHookedEntity entry)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnDeleting(TEntity entity, IHookedEntity entry)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnBeforeSaveCompleted(IEnumerable<IHookedEntity> entries)
        {
        }

        public virtual void OnAfterSave(IHookedEntity entry)
        {
            var entity = entry.Entity as TEntity;
            switch (entry.InitialState)
            {
                case EntityState.Added:
                    OnInserted(entity, entry);
                    break;
                case EntityState.Modified:
                    OnUpdated(entity, entry);
                    break;
                case EntityState.Deleted:
                    OnDeleted(entity, entry);
                    break;
            }
        }

        protected virtual void OnInserted(TEntity entity, IHookedEntity entry)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnUpdated(TEntity entity, IHookedEntity entry)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnDeleted(TEntity entity, IHookedEntity entry)
        {
            throw new NotImplementedException();
        }

        public virtual void OnAfterSaveCompleted(IEnumerable<IHookedEntity> entries)
        {
        }

        #endregion
    }
}
