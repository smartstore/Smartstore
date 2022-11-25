using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Data
{
    public class DbContextScope : Disposable
    {
        private readonly HookingDbContext _ctx;
        private readonly bool _autoDetectChangesEnabled;
        private readonly HookImportance _minHookImportance;
        private readonly bool _lazyLoadingEnabled;
        private readonly bool _retainConnection;
        private readonly bool _suppressCommit;
        private readonly QueryTrackingBehavior _queryTrackingBehavior;
        private readonly CascadeTiming _cascadeDeleteTiming;
        private readonly CascadeTiming _deleteOrphansTiming;

        /// <summary>
        /// Creates a scope in which a DbContext instance behaves differently. 
        /// The behaviour is resetted on disposal of the scope to what it was before.
        /// </summary>
        /// <param name="ctx">The context instance to change behavior for.</param>
        /// <param name="deferCommit">
        /// Suppresses the execution of <see cref="DbContext.SaveChanges()"/> / <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> 
        /// until this instance is disposed or <see cref="Commit()"/> / <see cref="CommitAsync(CancellationToken)"/> is called explicitly.
        /// </param>
        /// <param name="retainConnection">
        /// Opens connection and retains it until disposal. May increase load/save performance in large scopes.
        /// </param>
        public DbContextScope(HookingDbContext ctx,
            bool? autoDetectChanges = null,
            bool? lazyLoading = null,
            bool? forceNoTracking = null,
            bool? deferCommit = false,
            bool retainConnection = false,
            HookImportance? minHookImportance = null,
            CascadeTiming? cascadeDeleteTiming = null,
            CascadeTiming? deleteOrphansTiming = null)
        {
            Guard.NotNull(ctx, nameof(ctx));

            var changeTracker = ctx.ChangeTracker;

            _ctx = ctx;
            _autoDetectChangesEnabled = changeTracker.AutoDetectChangesEnabled;
            _minHookImportance = ctx.MinHookImportance;
            _suppressCommit = ctx.SuppressCommit;
            _lazyLoadingEnabled = changeTracker.LazyLoadingEnabled;
            _queryTrackingBehavior = changeTracker.QueryTrackingBehavior;
            _cascadeDeleteTiming = changeTracker.CascadeDeleteTiming;
            _deleteOrphansTiming = changeTracker.DeleteOrphansTiming;
            _retainConnection = retainConnection;

            if (autoDetectChanges.HasValue)
                changeTracker.AutoDetectChangesEnabled = autoDetectChanges.Value;

            if (minHookImportance.HasValue)
                ctx.MinHookImportance = minHookImportance.Value;

            if (lazyLoading.HasValue)
                changeTracker.LazyLoadingEnabled = lazyLoading.Value;

            if (forceNoTracking == true)
                changeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            if (deferCommit.HasValue)
                ctx.SuppressCommit = deferCommit.Value;

            if (cascadeDeleteTiming.HasValue)
                changeTracker.CascadeDeleteTiming = cascadeDeleteTiming.Value;

            if (deleteOrphansTiming.HasValue)
                changeTracker.DeleteOrphansTiming = deleteOrphansTiming.Value;

            if (retainConnection)
                ctx.Database.OpenConnection();
        }

        public HookingDbContext DbContext => _ctx;

        public Task LoadCollectionAsync<TEntity, TCollection>(
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TCollection>>> navigationProperty,
            bool force = false,
            Func<IQueryable<TCollection>, IQueryable<TCollection>> queryAction = null)
            where TEntity : BaseEntity
            where TCollection : BaseEntity
        {
            return _ctx.LoadCollectionAsync(entity, navigationProperty, force, queryAction);
        }

        public Task LoadReferenceAsync<TEntity, TProperty>(
            TEntity entity,
            Expression<Func<TEntity, TProperty>> navigationProperty,
            bool force = false)
            where TEntity : BaseEntity
            where TProperty : BaseEntity
        {
            return _ctx.LoadReferenceAsync(entity, navigationProperty, force);
        }

        /// <summary>
        /// Saves changes to database regardless of <c>deferCommit</c> parameter.
        /// </summary>
        public int Commit()
        {
            var suppressCommit = _ctx.SuppressCommit;

            try
            {
                _ctx.SuppressCommit = false;
                return _ctx.SaveChanges();
            }
            finally
            {
                _ctx.SuppressCommit = suppressCommit;
            }
        }

        /// <summary>
        /// Saves changes to database regardless of <c>deferCommit</c> parameter.
        /// </summary>
        public Task<int> CommitAsync(CancellationToken cancelToken = default)
        {
            var suppressCommit = _ctx.SuppressCommit;

            try
            {
                _ctx.SuppressCommit = false;
                return _ctx.SaveChangesAsync(cancelToken);
            }
            finally
            {
                _ctx.SuppressCommit = suppressCommit;
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                // Must come before ResetState()
                if (_ctx.SuppressCommit && _ctx.DeferCommit)
                    Commit();

                ResetState();

                if (_retainConnection && _ctx.Database.GetDbConnection().State == ConnectionState.Open)
                    _ctx.Database.CloseConnection();
            }
        }

        protected override async ValueTask OnDisposeAsync(bool disposing)
        {
            if (disposing)
            {
                // Must come before ResetState()
                if (_ctx.SuppressCommit && _ctx.DeferCommit)
                    await CommitAsync();

                ResetState();

                if (_retainConnection && _ctx.Database.GetDbConnection().State == ConnectionState.Open)
                    await _ctx.Database.CloseConnectionAsync();
            }
        }

        private void ResetState()
        {
            var changeTracker = _ctx.ChangeTracker;

            _ctx.MinHookImportance = _minHookImportance;
            _ctx.SuppressCommit = _suppressCommit;

            changeTracker.AutoDetectChangesEnabled = _autoDetectChangesEnabled;
            changeTracker.LazyLoadingEnabled = _lazyLoadingEnabled;
            changeTracker.QueryTrackingBehavior = _queryTrackingBehavior;
            changeTracker.CascadeDeleteTiming = _cascadeDeleteTiming;
            changeTracker.DeleteOrphansTiming = _deleteOrphansTiming;
        }
    }
}