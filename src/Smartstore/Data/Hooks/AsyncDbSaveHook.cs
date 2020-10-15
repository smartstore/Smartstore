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

        public virtual async Task OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var entity = entry.Entity as TEntity;
            switch (entry.InitialState)
            {
                case EntityState.Added:
                    await OnInsertingAsync(entity, entry, cancelToken);
                    break;
                case EntityState.Modified:
                    await OnUpdatingAsync(entity, entry, cancelToken);
                    break;
                case EntityState.Deleted:
                    await OnDeletingAsync(entity, entry, cancelToken);
                    break;
            }
        }

        public virtual async Task OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var entity = entry.Entity as TEntity;
            switch (entry.InitialState)
            {
                case EntityState.Added:
                    await OnInsertedAsync(entity, entry, cancelToken);
                    break;
                case EntityState.Modified:
                    await OnUpdatedAsync(entity, entry, cancelToken);
                    break;
                case EntityState.Deleted:
                    await OnDeletedAsync(entity, entry, cancelToken);
                    break;
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

        protected virtual Task OnInsertingAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        protected virtual Task OnUpdatingAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        protected virtual Task OnDeletingAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        protected virtual Task OnInsertedAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        protected virtual Task OnUpdatedAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        protected virtual Task OnDeletedAsync(TEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }
    }
}
