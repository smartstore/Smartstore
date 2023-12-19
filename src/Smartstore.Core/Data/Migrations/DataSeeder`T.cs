using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Caching;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Data;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public abstract class DataSeeder<TContext> : IDataSeeder<TContext>
        where TContext : HookingDbContext
    {
        private IUrlService _urlService;

        public DataSeeder(IApplicationContext appContext, ILogger logger)
        {
            ApplicationContext = Guard.NotNull(appContext);

            if (logger != null)
            {
                Logger = logger;
            }
        }

        /// <inheritdoc/>
        public virtual DataSeederStage Stage => DataSeederStage.Late;
        public virtual bool AbortOnFailure => false;

        /// <inheritdoc/>
        public Task SeedAsync(TContext context, CancellationToken cancelToken = default)
        {
            Context = Guard.NotNull(context);
            CancelToken = cancelToken;

            return SeedCoreAsync();
        }

        protected abstract Task SeedCoreAsync();

        protected TContext Context { get; set; }

        protected IApplicationContext ApplicationContext { get; set; }

        protected ILogger Logger { get; set; } = NullLogger.Instance;

        protected CancellationToken CancelToken { get; set; } = CancellationToken.None;

        #region Protected utils

        protected IUrlService UrlService
        {
            get
            {
                if (_urlService == null)
                {
                    var httpContextAccessor = ApplicationContext.Services.Resolve<IHttpContextAccessor>();

                    if (ApplicationContext.IsInstalled)
                    {
                        _urlService = httpContextAccessor.HttpContext?.RequestServices?.GetService<IUrlService>();
                    }

                    if (Context is not SmartDbContext db)
                    {
                        db = httpContextAccessor.HttpContext?.RequestServices?.GetService<SmartDbContext>();
                    }

                    _urlService ??= new UrlService(
                        db,
                        NullCache.Instance,
                        httpContextAccessor,
                        null, // IWorkContext not accessed
                        null, // IStoreContext not accessed
                        null, // ILanguageService not accessed
                        ApplicationContext.Services.Resolve<IRouteHelper>(),
                        new LocalizationSettings(),
                        new SeoSettings { LoadAllUrlAliasesOnStartup = false },
                        new PerformanceSettings(),
                        new SecuritySettings());
                }

                return _urlService;
            }
        }

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
            Guard.NotNull(populateAction);

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

        protected async Task PopulateUrlRecordsFor<T>(IEnumerable<T> entities, Func<T, UrlRecord> factory)
            where T : BaseEntity, ISlugSupported, new()
        {
            Guard.NotNull(entities);
            Guard.NotNull(factory);

            using var scope = UrlService.CreateBatchScope();

            foreach (var entity in entities)
            {
                var ur = factory(entity);
                if (ur != null)
                {
                    scope.ApplySlugs(new ValidateSlugResult
                    {
                        Source = entity,
                        Found = ur,
                        Slug = ur.Slug,
                        LanguageId = 0,
                        FoundIsSelf = true,
                    });
                }
            }

            await scope.CommitAsync();
        }

        protected string BuildSlug(string name)
            => SlugUtility.Slugify(name);

        protected Task SaveAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            Context.Set<TEntity>().Add(Guard.NotNull(entity, nameof(entity)));
            return Context.SaveChangesAsync();
        }

        protected async Task SaveRangeAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
        {
            Guard.NotNull(entities, nameof(entities));

            // INFO: chunk to avoid MySqlException "Error submitting ...MB packet; ensure 'max_allowed_packet' is greater than ...MB".
            foreach (var chunk in entities.Chunk(10))
            {
                Context.Set<TEntity>().AddRange(chunk);
                await Context.SaveChangesAsync();
            }
        }

        #endregion
    }
}
