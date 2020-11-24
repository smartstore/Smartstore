using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Smartstore.Domain;

namespace Smartstore.Data
{
    public class DbContextScope : IDisposable
    {
        private readonly HookingDbContext _ctx;
        private readonly bool _autoDetectChangesEnabled;
        private readonly bool _hooksEnabled;
        private readonly bool _lazyLoadingEnabled;
        private readonly QueryTrackingBehavior _queryTrackingBehavior;
        private readonly CascadeTiming _cascadeDeleteTiming;
        private readonly CascadeTiming _deleteOrphansTiming;
        private readonly bool _autoTransactionEnabled;
        private readonly bool _retainConnection;

        public DbContextScope(HookingDbContext ctx,
            bool? autoDetectChanges = null,
            bool? hooksEnabled = null,
            bool? lazyLoading = null,
            bool? forceNoTracking = null,
            CascadeTiming? cascadeDeleteTiming = null,
            CascadeTiming? deleteOrphansTiming = null,
            bool? autoTransactions = null,
            bool retainConnection = false)
        {
            Guard.NotNull(ctx, nameof(ctx));

            var changeTracker = ctx.ChangeTracker;

            _ctx = ctx;
            _autoDetectChangesEnabled = changeTracker.AutoDetectChangesEnabled;
            _hooksEnabled = ctx.HooksEnabled;
            _lazyLoadingEnabled = changeTracker.LazyLoadingEnabled;
            _queryTrackingBehavior = changeTracker.QueryTrackingBehavior;
            _cascadeDeleteTiming = changeTracker.CascadeDeleteTiming;
            _deleteOrphansTiming = changeTracker.DeleteOrphansTiming;
            _autoTransactionEnabled = ctx.Database.AutoTransactionsEnabled;
            _retainConnection = retainConnection;

            if (autoDetectChanges.HasValue)
                changeTracker.AutoDetectChangesEnabled = autoDetectChanges.Value;

            if (hooksEnabled.HasValue)
                ctx.HooksEnabled = hooksEnabled.Value;

            if (lazyLoading.HasValue)
                changeTracker.LazyLoadingEnabled = lazyLoading.Value;

            if (forceNoTracking == true)
                changeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            if (cascadeDeleteTiming.HasValue)
                changeTracker.CascadeDeleteTiming = cascadeDeleteTiming.Value;

            if (deleteOrphansTiming.HasValue)
                changeTracker.DeleteOrphansTiming = deleteOrphansTiming.Value;

            if (autoTransactions.HasValue)
                ctx.Database.AutoTransactionsEnabled = autoTransactions.Value;

            if (retainConnection)
                ctx.Database.OpenConnection();
        }

        public HookingDbContext DbContext => _ctx;

        public void LoadCollection<TEntity, TCollection>(
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TCollection>>> navigationProperty,
            bool force = false,
            Func<IQueryable<TCollection>, IQueryable<TCollection>> queryAction = null)
            where TEntity : BaseEntity
            where TCollection : BaseEntity
        {
            _ctx.LoadCollection(entity, navigationProperty, force, queryAction);
        }

        public void LoadReference<TEntity, TProperty>(
            TEntity entity,
            Expression<Func<TEntity, TProperty>> navigationProperty,
            bool force = false)
            where TEntity : BaseEntity
            where TProperty : BaseEntity
        {
            _ctx.LoadReference(entity, navigationProperty, force);
        }

        public int Commit()
        {
            return _ctx.SaveChanges();
        }

        public async Task<int> CommitAsync()
        {
            return await _ctx.SaveChangesAsync();
        }

        public void Dispose()
        {
            var changeTracker = _ctx.ChangeTracker;

            _ctx.HooksEnabled = _hooksEnabled;
            _ctx.Database.AutoTransactionsEnabled = _autoTransactionEnabled;

            changeTracker.AutoDetectChangesEnabled = _autoDetectChangesEnabled;
            changeTracker.LazyLoadingEnabled = _lazyLoadingEnabled;
            changeTracker.QueryTrackingBehavior = _queryTrackingBehavior;
            changeTracker.CascadeDeleteTiming = _cascadeDeleteTiming;
            changeTracker.DeleteOrphansTiming = _deleteOrphansTiming;

            if (_retainConnection && _ctx.Database.GetDbConnection().State == ConnectionState.Open)
                _ctx.Database.CloseConnection();
        }
    }
}