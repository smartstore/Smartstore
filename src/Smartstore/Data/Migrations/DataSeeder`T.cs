using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Domain;

namespace Smartstore.Data.Migrations
{
    public abstract class DataSeeder<TContext> : IDataSeeder<TContext>
        where TContext : HookingDbContext
    {
        public DataSeeder(ILogger logger)
        {
            Logger = logger;
        }

        /// <inheritdoc/>
        public bool RollbackOnFailure => false;

        /// <inheritdoc/>
        public Task SeedAsync(TContext context, CancellationToken cancelToken = default)
        {
            Context = Guard.NotNull(context, nameof(context));
            CancelToken = cancelToken;

            return SeedCoreAsync();
        }

        protected abstract Task SeedCoreAsync();

        protected TContext Context { get; set; }
        protected ILogger Logger { get; set; } = NullLogger.Instance;
        protected CancellationToken CancelToken { get; set; } = CancellationToken.None;

        protected async Task PopulateAsync<TEntity>(string stage, IEnumerable<TEntity> entities)
            where TEntity : BaseEntity
        {
            Guard.NotNull(entities, nameof(entities));

            if (!entities.Any())
                return;

            try
            {
                CancelToken.ThrowIfCancellationRequested();
                Logger.Debug("Populate: {0}", stage);
                await SaveRangeAsync(entities);
            }
            catch (Exception ex)
            {
                var ex2 = new SeedDataException(stage, ex);
                Logger.Error(ex2);
                throw ex2;
            }
        }

        protected async Task PopulateAsync(string stage, Func<Task> populateAction)
        {
            Guard.NotNull(populateAction, nameof(populateAction));

            try
            {
                CancelToken.ThrowIfCancellationRequested();
                Logger.Debug("Populate: {0}", stage.NaIfEmpty());
                await populateAction();
            }
            catch (Exception ex)
            {
                var ex2 = new SeedDataException(stage, ex);
                Logger.Error(ex2);
                throw ex2;
            }
        }

        protected void Populate(string stage, Action populateAction)
        {
            Guard.NotNull(populateAction, nameof(populateAction));

            try
            {
                CancelToken.ThrowIfCancellationRequested();
                Logger.Debug("Populate: {0}", stage.NaIfEmpty());
                populateAction();
            }
            catch (Exception ex)
            {
                var ex2 = new SeedDataException(stage, ex);
                Logger.Error(ex2);
                throw ex2;
            }
        }

        protected Task SaveAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            Context.Set<TEntity>().Add(Guard.NotNull(entity, nameof(entity)));
            return Context.SaveChangesAsync();
        }

        protected Task SaveRangeAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
        {
            Context.Set<TEntity>().AddRange(Guard.NotNull(entities, nameof(entities)));
            return Context.SaveChangesAsync();
        }
    }
}
