using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Smartstore.Data.Hooks;
using Smartstore.Data.Providers;
using Smartstore.Domain;
using Smartstore.Engine;
using Smartstore.Utilities;

namespace Smartstore.Data
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    public abstract class HookingDbContext : DbContext
    {
        private static readonly FieldInfo _leaseField = typeof(DbContext).GetField("_lease", BindingFlags.NonPublic | BindingFlags.Instance);
        
        private static readonly ValueConverter _dateTimeConverter =
            new ValueConverter<DateTime, DateTime>(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        private static readonly ValueConverter _nullableDateTimeConverter =
            new ValueConverter<DateTime?, DateTime?>(
                v => v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        private DbContextLease? _lease;
        private readonly Stack<DbSaveChangesOperation> _saveOperations = new(2);
        private DataProvider _dataProvider;

        public HookingDbContext(DbContextOptions options)
            : base(options)
        {
            Options = options;

            if (CommonHelper.IsHosted)
            {
                ChangeTracker.Tracked += OnTracked;
                ChangeTracker.StateChanged += OnStateChanged;
            }
        }

        #region LazyLoader injection

        private static void OnTracked(object sender, EntityTrackedEventArgs e)
        {
            var entry = e.Entry;
            if (entry.Entity is BaseEntity entity && entry.State is EfState.Unchanged or EfState.Modified)
            {
                InjectLazyLoader(entity, entry.Context);
            }
        }

        private static void OnStateChanged(object sender, EntityStateChangedEventArgs e)
        {
            var entry = e.Entry;
            if (entry.Entity is BaseEntity entity)
            {
                if (e.NewState is EfState.Unchanged or EfState.Modified)
                {
                    InjectLazyLoader(entity, entry.Context);
                }
                else
                {
                    DropLazyLoader(entity);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InjectLazyLoader(BaseEntity entity, DbContext db)
        {
            if (entity.LazyLoader is NullLazyLoader)
            {
                var lazyLoader = db.GetService<ILazyLoader>();
                entity.LazyLoader = lazyLoader;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DropLazyLoader(BaseEntity entity)
        {
            if (entity.LazyLoader is LazyLoader lazyLoader)
            {
                lazyLoader.Dispose();
                entity.LazyLoader = NullLazyLoader.Instance;
            }
        }

        #endregion

        /// <summary>
        /// Gets a value indicating whether the DbContext instance
        /// was obtained from the DbContext pool.
        /// </summary>
        protected virtual bool IsActiveLease 
        { 
            get
            {
                _lease ??= (DbContextLease)_leaseField.GetValue(this);
                _lease ??= DbContextLease.InactiveLease;

                return _lease.Value.IsActive == true;
            }
        }

        protected internal virtual DbContextOptions Options { get; }

        public DataProvider DataProvider
        {
            get
            {
                if (_dataProvider == null || _dataProvider.IsDisposed)
                {
                    _dataProvider = DataSettings.Instance.DbFactory.CreateDataProvider(this.Database);
                }

                return _dataProvider;
            }
        }

        public override void Dispose()
        {
            ResetState();
            base.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            ResetState();
            return base.DisposeAsync();
        }

        private void ResetState()
        {
            while (_saveOperations.TryPop(out var op))
            {
                op.Dispose();
            }

            if (_dataProvider != null)
            {
                _dataProvider.Dispose();
                _dataProvider = null;
            }

            if (IsActiveLease)
            {
                // Instance is returned to pool: reset state.
                MinHookImportance = HookImportance.Normal;
                SuppressCommit = false;
                DeferCommit = false;

                var trackedEntries = ChangeTracker.Entries<BaseEntity>();
                foreach (var entry in trackedEntries)
                {
                    if (entry.Entity.LazyLoader is LazyLoader lazyLoader)
                    {
                        lazyLoader.Dispose();
                        entry.Entity.LazyLoader = null;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Type GetInvariantType()
            => GetType();

        #region Save

        /// <summary>
        /// Gets or sets the minimum importance level of executable hooks. Only hooks
        /// with level equal or higher than the current value will be executed.
        /// </summary>
        public HookImportance MinHookImportance { get; set; } = HookImportance.Normal;

        /// <summary>
        /// DON'T SET THIS TO TRUE. It's only meant to be working within a <see cref="DbContextScope"/> instance.
        /// </summary>
        internal bool SuppressCommit { get; set; }

        /// <summary>
        /// Internal API meant to be working within a <see cref="DbContextScope"/> instance only.
        /// </summary>
        internal bool DeferCommit { get; set; }

        protected internal IDbHookProcessor ActivateHookProcessor()
        {
            try
            {
                return EngineContext.Current?.Scope?.ResolveOptional<IDbHookProcessor>() ?? NullDbHookProcessor.Instance;
            }
            catch
            {
                return NullDbHookProcessor.Instance;
            }
        }

        protected internal bool IsInSaveOperation => _saveOperations.Count > 0;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
            => SaveChangesInternal(acceptAllChangesOnSuccess, false).GetAwaiter().GetResult();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            => SaveChangesInternal(acceptAllChangesOnSuccess, true, cancellationToken);

        private async Task<int> SaveChangesInternal(bool acceptAllChangesOnSuccess, bool async, CancellationToken cancelToken = default)
        {
            if (SuppressCommit)
            {
                DeferCommit = true;
                return 0;
            }
            else
            {
                DeferCommit = false;
            }

            _saveOperations.TryPeek(out var currentSaveOperation);

            if (currentSaveOperation == null)
            {
                // No operation currently running. Create a new operation.
                currentSaveOperation = new DbSaveChangesOperation(this, ActivateHookProcessor());
            }
            else
            {
                if (currentSaveOperation.Stage == DbSaveStage.PreSave)
                {
                    // This was called from within a PRE action hook. We must get out:... 
                    // 1.) to prevent cyclic calls
                    // 2.) we want new entities in the state tracker (added by pre hooks) to be committed atomically in the core SaveChanges() call later on.
                    return 0;
                }
                else if (currentSaveOperation.Stage == DbSaveStage.PostSave)
                {
                    // This was called from within a POST action hook. Core SaveChanges() has already been called,
                    // but new entities could have been added to the state tracker by hooks.
                    // Therefore we allow a new nested save operation where only ESSENTIAL PRE hooks may run.
                    currentSaveOperation = new DbSaveChangesOperation(currentSaveOperation);
                }
            }

            _saveOperations.Push(currentSaveOperation);

            try
            {
                if (async)
                {
                    return await currentSaveOperation.ExecuteAsync(acceptAllChangesOnSuccess, cancelToken);
                }
                else
                {
                    return currentSaveOperation.Execute(acceptAllChangesOnSuccess);
                }
            }
            finally
            {
                _saveOperations.TryPop(out currentSaveOperation);
                currentSaveOperation?.Dispose();
            }
        }

        /// <summary>
        /// Just calls <see cref="SaveChanges(bool)"/> without any sugar
        /// </summary>
        /// <returns>The number of affected records</returns>
        protected internal int SaveChangesCore(bool acceptAllChangesOnSuccess)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        /// <summary>
        /// Just calls <see cref="SaveChangesAsync(bool, CancellationToken)(bool)"/> without any sugar
        /// </summary>
        /// <returns>The number of affected records</returns>
        protected internal Task<int> SaveChangesCoreAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        #endregion

        #region Model bootstrapping

        protected void CreateModel(ModelBuilder modelBuilder, IEnumerable<Assembly> assemblies)
        {
            Guard.NotNull(assemblies);

            RegisterEntities(modelBuilder, assemblies);
            RegisterEntityMappings(modelBuilder, assemblies);
            ApplyConventions(modelBuilder);
        }

        private static void RegisterEntities(ModelBuilder modelBuilder, IEnumerable<Assembly> assemblies)
        {
            var entityTypes = assemblies
                .SelectMany(x => x.GetExportedTypes())
                .Where(x => typeof(BaseEntity).IsAssignableFrom(x) && !x.IsAbstract && x.HasDefaultConstructor())
                .ToList();

            foreach (var type in entityTypes)
            {
                modelBuilder.Entity(type);
            }
        }

        private static void RegisterEntityMappings(ModelBuilder modelBuilder, IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                modelBuilder.ApplyConfigurationsFromAssembly(assembly);
            }
        }

        private static void ApplyConventions(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                ApplySingularTableNameConvention(entityType);

                var properties = entityType.GetProperties();
                foreach (var property in properties)
                {
                    // decimal HasPrecision(18, 4) convention
                    ApplyDecimalPrecisionConvention(property);

                    // DateTime UTC convention.
                    ApplyDateTimeUtcConvention(property);
                }
            }
        }

        private static void ApplySingularTableNameConvention(IMutableEntityType entityType)
        {
            if (entityType.IsPropertyBag)
            {
                return;
            }

            if (entityType.BaseType != null)
            {
                // TPH inheritance: derived type maps to base type table.
                return;
            }

            var conventionAnnotation = entityType.FindAnnotation(RelationalAnnotationNames.TableName) as IConventionAnnotation;
            if (conventionAnnotation == null || conventionAnnotation.GetConfigurationSource() == ConfigurationSource.Convention)
            {
                // Apply table name convention only when no convention exists or existing convention is "Conventional" (NOT Explicit or DataAnnotation).
                entityType.SetTableName(entityType.DisplayName());
            }
        }

        private static void ApplyDecimalPrecisionConvention(IMutableProperty property)
        {
            if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
            {
                var precisionAnnotation = property.FindAnnotation(CoreAnnotationNames.Precision) as IConventionAnnotation;
                if (precisionAnnotation == null || precisionAnnotation.GetConfigurationSource() == ConfigurationSource.Convention)
                {
                    // Apply precision convention only when no convention exists or existing convention is "Conventional" (NOT Explicit or DataAnnotation).
                    property.SetPrecision(18);
                }

                var scaleAnnotation = property.FindAnnotation(CoreAnnotationNames.Scale) as IConventionAnnotation;
                if (scaleAnnotation == null || scaleAnnotation.GetConfigurationSource() == ConfigurationSource.Convention)
                {
                    // Apply scale convention only when no convention exists or existing convention is "Conventional" (NOT Explicit or DataAnnotation).
                    property.SetScale(4);
                }
            }
        }

        private static void ApplyDateTimeUtcConvention(IMutableProperty property)
        {
            if (property.ClrType == typeof(DateTime) && CanConvert())
            {
                property.SetValueConverter(_dateTimeConverter);
            }
            else if (property.ClrType == typeof(DateTime?) && CanConvert())
            {
                property.SetValueConverter(_nullableDateTimeConverter);
            }

            bool CanConvert()
            {
                // Not all DateTime properties contain UTC values (e.g. Customer.BirthDate), so we only convert those whose names end in "Utc".
                if (property.Name.EndsWith("Utc"))
                {
                    if (property.FindAnnotation(CoreAnnotationNames.ValueConverter) is not IConventionAnnotation converterAnnotation
                        || converterAnnotation.GetConfigurationSource() == ConfigurationSource.Convention)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion
    }
}
