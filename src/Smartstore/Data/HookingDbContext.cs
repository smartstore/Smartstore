using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using Smartstore.Engine;

namespace Smartstore.Data
{
    public abstract class HookingDbContext : DbContext
    {
        private DbSaveChangesOperation _currentSaveOperation;
        private DataProvider _dataProvider;

        public HookingDbContext(DbContextOptions options)
            : base(options)
        {
        }

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
            // Instance is returned to pool: reset state.
            HooksEnabled = true;
            SuppressCommit = false;
            DeferCommit = false;
            _currentSaveOperation = null;

            if (_dataProvider != null)
            {
                _dataProvider.Dispose();
                _dataProvider = null;
            }
        }

        #region Save

        public bool HooksEnabled { get; set; } = true;

        /// <summary>
        /// DON'T SET THIS TO TRUE. It's only meant to be working within a <see cref="DbContextScope"/> instance.
        /// </summary>
        internal bool SuppressCommit { get; set; }

        /// <summary>
        /// Internal API meant to be working within a <see cref="DbContextScope"/> instance only.
        /// </summary>
        internal bool DeferCommit { get; set; }

        [SuppressMessage("Performance", "CA1822:Member can be static", Justification = "Seriously?")]
        protected internal IDbHookHandler DbHookHandler
        {
            get => EngineContext.Current.Scope.ResolveOptional<IDbHookHandler>() ?? NullDbHookHandler.Instance;
        }

        protected internal bool IsInSaveOperation => _currentSaveOperation != null;

        /// <inheritdoc/>
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
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

            var op = _currentSaveOperation;

            if (op != null)
            {
                if (op.Stage == DbSaveStage.PreSave)
                {
                    // This was called from within a PRE action hook. We must get out:... 
                    // 1.) to prevent cyclic calls
                    // 2.) we want new entities in the state tracker (added by pre hooks) to be committed atomically in the core SaveChanges() call later on.
                    return 0;
                }
                else if (op.Stage == DbSaveStage.PostSave)
                {
                    // This was called from within a POST action hook. Core SaveChanges() has already been called,
                    // but new entities could have been added to the state tracker by hooks.
                    // Therefore we need to commit them and get outta here, otherwise: cyclic nightmare!
                    // DetectChanges() here is important, 'cause we turned it off for the save process.
                    ChangeTracker.DetectChanges();
                    return SaveChangesCore(acceptAllChangesOnSuccess);
                }
            }

            _currentSaveOperation = new DbSaveChangesOperation(this, this.DbHookHandler);

            try
            {
                return _currentSaveOperation.Execute(acceptAllChangesOnSuccess);
            }
            finally
            {
                _currentSaveOperation?.Dispose();
                _currentSaveOperation = null;
            }
        }

        /// <inheritdoc/>
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
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

            var op = _currentSaveOperation;

            if (op != null)
            {
                if (op.Stage == DbSaveStage.PreSave)
                {
                    // This was called from within a PRE action hook. We must get out:... 
                    // 1.) to prevent cyclic calls
                    // 2.) we want new entities in the state tracker (added by pre hooks) to be committed atomically in the core SaveChanges() call later on.
                    return 0;
                }
                else if (op.Stage == DbSaveStage.PostSave)
                {
                    // This was called from within a POST action hook. Core SaveChanges() has already been called,
                    // but new entities could have been added to the state tracker by hooks.
                    // Therefore we need to commit them and get outta here, otherwise: cyclic nightmare!
                    // DetectChanges() here is important, 'cause we turned it off for the save process.
                    base.ChangeTracker.DetectChanges();
                    return await SaveChangesCoreAsync(acceptAllChangesOnSuccess, cancellationToken);
                }
            }

            _currentSaveOperation = new DbSaveChangesOperation(this, this.DbHookHandler);

            try
            {
                return await _currentSaveOperation.ExecuteAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            finally
            {
                _currentSaveOperation?.Dispose();
                _currentSaveOperation = null;
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

        protected void CreateModel(ModelBuilder modelBuilder, Assembly assembly)
        {
            Guard.NotNull(assembly, nameof(assembly));

            RegisterEntities(modelBuilder, assembly);
            RegisterEntityMappings(modelBuilder, assembly);
            ApplyConventions(modelBuilder);
        }

        private static void RegisterEntities(ModelBuilder modelBuilder, Assembly assembly)
        {
            var entityTypes = assembly.GetExportedTypes()
                .Where(x => typeof(BaseEntity).IsAssignableFrom(x) && !x.IsAbstract && x.HasDefaultConstructor())
                .ToList();

            foreach (var type in entityTypes)
            {
                // TODO: (core) Are we safe to add an entity model twice? ('cause EF did this already for publicly declared DbSet properties in SmartDbContext)
                modelBuilder.Entity(type);
            }
        }

        private static void RegisterEntityMappings(ModelBuilder modelBuilder, Assembly assembly)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        private static void ApplyConventions(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // TODO: (core) Add more proper conventions (set StringLength to 4000 by default?)
                // TODO: (core) Make provider for conventions

                ApplySingularTableNameConvention(entityType);

                var decimalProperties = entityType.GetProperties();
                foreach (var property in decimalProperties)
                {
                    // decimal HasPrecision(18, 4) convention
                    ApplyDecimalPrecisionConvention(property);
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

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Required")]
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

        #endregion
    }
}
