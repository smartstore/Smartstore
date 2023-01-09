using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Data
{
    public class DbContextScope : Disposable
    {
        private readonly HookingDbContext _db;
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
        /// <param name="db">The context instance to change behavior for.</param>
        /// <param name="deferCommit">
        /// Suppresses the execution of <see cref="DbContext.SaveChanges()"/> / <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> 
        /// until this instance is disposed or <see cref="Commit()"/> / <see cref="CommitAsync(CancellationToken)"/> is called explicitly.
        /// </param>
        /// <param name="retainConnection">
        /// Opens connection and retains it until disposal. May increase load/save performance in large scopes.
        /// </param>
        public DbContextScope(HookingDbContext db,
            bool? autoDetectChanges = null,
            bool? lazyLoading = null,
            bool? forceNoTracking = null,
            bool? deferCommit = false,
            bool retainConnection = false,
            HookImportance? minHookImportance = null,
            CascadeTiming? cascadeDeleteTiming = null,
            CascadeTiming? deleteOrphansTiming = null)
        {
            _db = Guard.NotNull(db);

            var changeTracker = db.ChangeTracker;

            _autoDetectChangesEnabled = changeTracker.AutoDetectChangesEnabled;
            _minHookImportance = db.MinHookImportance;
            _suppressCommit = db.SuppressCommit;
            _lazyLoadingEnabled = changeTracker.LazyLoadingEnabled;
            _queryTrackingBehavior = changeTracker.QueryTrackingBehavior;
            _cascadeDeleteTiming = changeTracker.CascadeDeleteTiming;
            _deleteOrphansTiming = changeTracker.DeleteOrphansTiming;
            _retainConnection = retainConnection;

            if (autoDetectChanges.HasValue)
                changeTracker.AutoDetectChangesEnabled = autoDetectChanges.Value;

            if (minHookImportance.HasValue)
                db.MinHookImportance = minHookImportance.Value;

            if (lazyLoading.HasValue)
                changeTracker.LazyLoadingEnabled = lazyLoading.Value;

            if (forceNoTracking == true)
                changeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            if (deferCommit.HasValue)
                db.SuppressCommit = deferCommit.Value;

            if (cascadeDeleteTiming.HasValue)
                changeTracker.CascadeDeleteTiming = cascadeDeleteTiming.Value;

            if (deleteOrphansTiming.HasValue)
                changeTracker.DeleteOrphansTiming = deleteOrphansTiming.Value;

            if (retainConnection)
                db.Database.OpenConnection();
        }

        public HookingDbContext DbContext => _db;

        public Task LoadCollectionAsync<TEntity, TCollection>(
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TCollection>>> navigationProperty,
            bool force = false,
            Func<IQueryable<TCollection>, IQueryable<TCollection>> queryAction = null)
            where TEntity : BaseEntity
            where TCollection : BaseEntity
        {
            return _db.LoadCollectionAsync(entity, navigationProperty, force, queryAction);
        }

        public Task LoadReferenceAsync<TEntity, TProperty>(
            TEntity entity,
            Expression<Func<TEntity, TProperty>> navigationProperty,
            bool force = false)
            where TEntity : BaseEntity
            where TProperty : BaseEntity
        {
            return _db.LoadReferenceAsync(entity, navigationProperty, force);
        }

        /// <summary>
        /// Saves changes to database regardless of <c>deferCommit</c> parameter.
        /// </summary>
        public int Commit()
        {
            var suppressCommit = _db.SuppressCommit;

            try
            {
                _db.SuppressCommit = false;
                return _db.SaveChanges();
            }
            finally
            {
                _db.SuppressCommit = suppressCommit;
            }
        }

        /// <summary>
        /// Saves changes to database regardless of <c>deferCommit</c> parameter.
        /// </summary>
        public Task<int> CommitAsync(CancellationToken cancelToken = default)
        {
            var suppressCommit = _db.SuppressCommit;

            try
            {
                _db.SuppressCommit = false;
                return _db.SaveChangesAsync(cancelToken);
            }
            finally
            {
                _db.SuppressCommit = suppressCommit;
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                // Must come before ResetState()
                if (_db.SuppressCommit && _db.DeferCommit)
                    Commit();

                ResetState();

                if (_retainConnection && _db.Database.GetDbConnection().State == ConnectionState.Open)
                    _db.Database.CloseConnection();
            }
        }

        protected override async ValueTask OnDisposeAsync(bool disposing)
        {
            if (disposing)
            {
                // Must come before ResetState()
                if (_db.SuppressCommit && _db.DeferCommit)
                    await CommitAsync();

                ResetState();

                if (_retainConnection && _db.Database.GetDbConnection().State == ConnectionState.Open)
                    await _db.Database.CloseConnectionAsync();
            }
        }

        private void ResetState()
        {
            var changeTracker = _db.ChangeTracker;

            _db.MinHookImportance = _minHookImportance;
            _db.SuppressCommit = _suppressCommit;

            changeTracker.AutoDetectChangesEnabled = _autoDetectChangesEnabled;
            changeTracker.LazyLoadingEnabled = _lazyLoadingEnabled;
            changeTracker.QueryTrackingBehavior = _queryTrackingBehavior;
            changeTracker.CascadeDeleteTiming = _cascadeDeleteTiming;
            changeTracker.DeleteOrphansTiming = _deleteOrphansTiming;
        }
    }
}